namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CollectFormStep"/>.
/// Supports three ways to define a form schema:
/// <list type="number">
/// <item><description><c>schema</c>: reference by name via <see cref="IFormSchemaProvider"/>.</description></item>
/// <item><description><c>schemaDef</c>: inline schema in the step JSON.</description></item>
/// <item><description><c>schemaPatch</c>: patch (overlay) on the base schema — sections <c>add</c>/<c>remove</c>/<c>override</c>/<c>rename</c> and optional <c>name</c>.</description></item>
/// </list>
/// Step example:
/// <code language="json">
/// {
///   "kind": "collectForm",
///   "name": "auth-form",
///   "schemaDef": {
///     "name": "auth",
///     "fields": [ { "key": "Email", "type": "Email" } ]
///   },
///   "schemaPatch": {
///     "add": [ { "key": "OtpCode", "type": "String", "required": true, "min": 6, "max": 12 } ],
///     "override": [ { "key": "Email", "regex": "^[^@]+@example\\.com$" } ],
///     "remove": [ "LegacyField" ],
///     "rename": [ { "from": "Email", "to": "Login" } ],
///     "name": "auth2"
///   },
///   "next": "verify"
/// }
/// </code>
/// </summary>
internal sealed class CollectFormStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        // optional: if JSON still contains "kind", validate it matches
        StepFactoryJsonGuards.ValidateOptionalKind(cfg, Kind);

        var next = cfg.Str("next");

        // 1) base schema: by name or inline
        var schema = ParseSchema(cfg, sp);

        // 2) optional patch
        if (cfg.TryGetProperty("schemaPatch", out var patch) && patch.ValueKind == JsonValueKind.Object)
            schema = ApplyPatch(schema, patch);

        // 3) validator
        var validatorFactory = sp.GetRequiredService<IFormValidatorFactory>();
        var validator = validatorFactory.Create(schema);

        // 4) request input source (from the controller)
        var input = sp.GetRequiredService<IRequestInput>();
        ArgumentException.ThrowIfNullOrEmpty(nameof(input));

        return new CollectFormStep
        {
            Kind          = Kind,
            Schema        = schema,
            Validator     = validator,
            FetchIncoming = input.GetAsync,
            Selector      = Selector.TryFromStepJson(cfg),
            Next          = next
        };
    }

    /// <summary>
    /// Build <see cref="FormSchema"/> either by name (<c>schema</c>) or from an inline definition (<c>schemaDef</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">When the schema is not specified or the provider is missing.</exception>
    private FormSchema ParseSchema(JsonElement cfg, IServiceProvider sp)
    {
        // Option A: schema: "name"
        if (cfg.TryGetProperty("schema", out var schemaNameEl) && schemaNameEl.ValueKind == JsonValueKind.String)
        {
            var schemaName = schemaNameEl.GetString()!;
            var provider = sp.GetService<IFormSchemaProvider>()
                ?? throw new InvalidOperationException("IFormSchemaProvider is not registered but 'schema' by name was used.");
            return provider.Get(schemaName);
        }

        // Option B: schemaDef: { fields:[], [validators:[]] }
        if (cfg.TryGetProperty("schemaDef", out var defEl) && defEl.ValueKind == JsonValueKind.Object)
        {
            // fields
            if (!defEl.TryGetProperty("fields", out var fieldsEl) || fieldsEl.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("collectForm.schemaDef requires 'fields' array.");

            var fields = new List<FieldDescriptor>();
            foreach (var f in fieldsEl.EnumerateArray())
            {
                var key    = f.GetProperty("key").GetString()!;
                var type         = ParseFieldType(f.GetProperty("type").GetString()!);
                var required= f.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
                int? min         = f.TryGetProperty("min", out var minEl) && minEl.ValueKind == JsonValueKind.Number ? minEl.GetInt32() : null;
                int? max         = f.TryGetProperty("max", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number ? maxEl.GetInt32() : null;
                string? rx       = f.TryGetProperty("regex", out var rxEl)  && rxEl.ValueKind == JsonValueKind.String ? rxEl.GetString() : null;

                fields.Add(new FieldDescriptor(key, type, required, min, max, rx));
            }

            // validators (optional)
            var validators = new List<IFormSchemaRule>();
            if (defEl.TryGetProperty("validators", out var validatorsEl) && validatorsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in validatorsEl.EnumerateArray())
                {
                    if (v.ValueKind != JsonValueKind.Object || !v.TryGetProperty("kind", out var kindEl))
                        continue;

                    var vk = kindEl.GetString();
                    switch (vk)
                    {
                        case "equal":
                            {
                                var left  = v.GetProperty("left").GetString()!;
                                var right = v.GetProperty("right").GetString()!;
                                var msg   = v.StrOpt("message");
                                validators.Add(new EqualFieldsRule(left, right, msg));
                            }
                            break;

                        case "notEqual":
                            {
                                var left  = v.GetProperty("left").GetString()!;
                                var right = v.GetProperty("right").GetString()!;
                                var msg   = v.StrOpt("message");
                                validators.Add(new NotEqualFieldsRule(left, right, msg));
                            }
                            break;

                        case "oneOf":
                            {
                                var name   = v.GetProperty("name").GetString()!;
                                var arr    = v.GetProperty("allowed").EnumerateArray().Select(e => e.GetString()!).ToArray();
                                var msg    = v.StrOpt("message");
                                validators.Add(new OneOfRule(name, arr, msg));
                            }
                            break;

                        case "requiredIf":
                            {
                                var whenEl = v.GetProperty("when");
                                var thenEl = v.GetProperty("then");

                                var whenField = whenEl.GetProperty("field").GetString()!;

                                // equals:
                                // - if not specified at all → special mode "field is NOT empty"
                                // - if specified (including empty string "") → exact string comparison
                                string? whenEquals = null;
                                if (whenEl.TryGetProperty("equals", out var eqEl))
                                {
                                    if (eqEl.ValueKind == JsonValueKind.String)
                                    {
                                        // keep the string as-is, including ""
                                        whenEquals = eqEl.GetString();
                                    }
                                    else if (eqEl.ValueKind == JsonValueKind.Null)
                                    {
                                        whenEquals = null;
                                    }
                                }

                                var thenName = thenEl.GetProperty("name").GetString()!;
                                var thenReq = thenEl.TryGetProperty("required", out var reqEl) && reqEl.ValueKind == JsonValueKind.True;

                                var msg = v.StrOpt("message");
                                validators.Add(new RequiredIfRule((whenField, whenEquals), (thenName, thenReq), msg));
                            }
                            break;

                        case "exactlyOneRequired":
                            {
                                var fields_ = v.GetProperty("fields").EnumerateArray().Select(e => e.GetString()!).ToArray();
                                var msg    = v.StrOpt("message");
                                validators.Add(new ExactlyOneRequiredRule(fields_, msg));
                            }
                            break;

                        case "atLeastOneRequired":
                            {
                                var fields_ = v.GetProperty("fields").EnumerateArray().Select(e => e.GetString()!).ToArray();
                                var msg    = v.StrOpt("message");
                                validators.Add(new AtLeastOneRequiredRule(fields_, msg));
                            }
                            break;
                    }
                }
            }

            // schema name is internal only; Bag prefix is set by step Kind
            var schemaName = cfg.Str("kind");
            return new FormSchema(schemaName, fields, validators);
        }

        throw new InvalidOperationException("collectForm requires either 'schema' (name) or 'schemaDef' (inline).");
    }

    /// <summary>
    /// Apply <c>schemaPatch</c> to the base schema.
    /// Supported sections: <c>remove</c>, <c>override</c>, <c>add</c>, <c>rename</c>, and optional <c>name</c>.
    /// </summary>
    private static FormSchema ApplyPatch(FormSchema baseSchema, JsonElement patch)
    {
        var fields = baseSchema.Fields.ToList();

        // remove: list of keys to delete
        if (patch.TryGetProperty("remove", out var rem) && rem.ValueKind == JsonValueKind.Array)
        {
            var toRemove = rem.EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);
            fields.RemoveAll(f => toRemove.Contains(f.Key));
        }

        // override: change properties of existing fields (min/max/required/regex)
        if (patch.TryGetProperty("override", out var ov) && ov.ValueKind == JsonValueKind.Array)
        {
            foreach (var o in ov.EnumerateArray())
            {
                var key = o.GetProperty("key").GetString()!;
                var i = fields.FindIndex(f => f.Key == key);
                if (i < 0) continue;

                var curr = fields[i];
                int? min = o.TryGetProperty("min", out var minEl) && minEl.ValueKind == JsonValueKind.Number ? minEl.GetInt32() : curr.Min;
                int? max = o.TryGetProperty("max", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number ? maxEl.GetInt32() : curr.Max;
                bool required = o.TryGetProperty("required", out var rEl) ? rEl.ValueKind == JsonValueKind.True : curr.Required;
                string? rx = o.TryGetProperty("regex", out var rxEl) && rxEl.ValueKind == JsonValueKind.String ? rxEl.GetString() : curr.Regex;

                fields[i] = curr with { Min = min, Max = max, Required = required, Regex = rx };
            }
        }

        // add: add new fields (or replace when the key matches)
        if (patch.TryGetProperty("add", out var add) && add.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in add.EnumerateArray())
            {
                var key = f.GetProperty("key").GetString()!;
                var type = ParseFieldType(f.GetProperty("type").GetString()!);
                var required = f.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
                int? min = f.TryGetProperty("min", out var minEl) && minEl.ValueKind == JsonValueKind.Number ? minEl.GetInt32() : null;
                int? max = f.TryGetProperty("max", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number ? maxEl.GetInt32() : null;
                string? rx = f.TryGetProperty("regex", out var rxEl) && rxEl.ValueKind == JsonValueKind.String ? rxEl.GetString() : null;

                var idx = fields.FindIndex(x => x.Key == key);
                var desc = new FieldDescriptor(key, type, required, min, max, rx);
                if (idx >= 0) fields[idx] = desc; else fields.Add(desc);
            }
        }

        // rename: rename field keys
        if (patch.TryGetProperty("rename", out var ren) && ren.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in ren.EnumerateArray())
            {
                var from = r.GetProperty("from").GetString()!;
                var to = r.GetProperty("to").GetString()!;
                var idx = fields.FindIndex(x => x.Key == from);
                if (idx >= 0) fields[idx] = fields[idx] with { Key = to };
            }
        }

        // name: optionally rename the entire schema (does NOT affect bag prefix; prefix = step Name)
        var name = patch.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
            ? n.GetString()! : baseSchema.Name;

        return new FormSchema(name, fields);
    }

    /// <summary>Parse a string field type into <see cref="FieldTypeEnum"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown field type.</exception>
    private static FieldTypeEnum ParseFieldType(string s) => s.ToLowerInvariant() switch
    {
        "string"      => FieldTypeEnum.String,
        "int"         => FieldTypeEnum.Int,
        "email"       => FieldTypeEnum.Email,
        "phonenumber" => FieldTypeEnum.PhoneNumber,
        "password"    => FieldTypeEnum.Password,
        "date"        => FieldTypeEnum.Date,
        "bool"        => FieldTypeEnum.Bool,
        "timespan"    => FieldTypeEnum.TimeSpan,
        _ => throw new ArgumentOutOfRangeException(nameof(s), $"Unknown field type '{s}'.")
    };
}
