namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories;

/// <summary>
/// Универсальная реализация валидатора форм, сочетающая базовую и расширенную валидацию.
/// </summary>
public sealed class UnifiedFormValidatorFactory : IFormValidatorFactory
{
    public IValidator<IDictionary<string, object?>> Create(FormSchema schema)
    {
        var validator = new InlineValidator<IDictionary<string, object?>>();

        // Валидация полей
        validator.RuleFor(x => x).Custom((dict, ctx) =>
        {
            foreach (var field in schema.Fields)
            {
                ValidateField(field, dict, ctx);
            }
        });

        // Межполевая валидация
        foreach (var rule in schema.Validators)
        {
            AddCrossFieldValidation(validator, rule);
        }

        return validator;
    }

    private static void ValidateField(FieldDescriptor field, IDictionary<string, object?> dict, ValidationContext<IDictionary<string, object?>> ctx)
    {
        var hasValue = dict.TryGetValue(field.Key, out var value);
        var stringValue = value?.ToString();

        // Проверка обязательности
        if (field.Required && (!hasValue || value is null || IsEmpty(value)))
        {
            ctx.AddFailure(field.Key, $"Field '{field.Key}' is required.");
            return;
        }

        if (!hasValue || value is null) return;

        // Проверка длины
        if (stringValue is not null)
        {
            if (field.Min is not null && stringValue.Length < field.Min.Value)
                ctx.AddFailure(field.Key, $"Field '{field.Key}' must be at least {field.Min} characters long.");
            if (field.Max is not null && stringValue.Length > field.Max.Value)
                ctx.AddFailure(field.Key, $"Field '{field.Key}' must be at most {field.Max} characters long.");
        }

        // Проверка типов
        switch (field.Type)
        {
            case FieldTypeEnum.Email:
                if (!LooksLikeEmail(stringValue))
                    ctx.AddFailure(field.Key, $"Field '{field.Key}' must be a valid email.");
                break;

            case FieldTypeEnum.Phone:
                if (!IsValidPhone(stringValue))
                    ctx.AddFailure(field.Key, $"Field '{field.Key}' must be a valid phone number.");
                break;

            case FieldTypeEnum.Int:
                if (!int.TryParse(stringValue, out _))
                    ctx.AddFailure(field.Key, $"Field '{field.Key}' must be a int value.");
                break;

            case FieldTypeEnum.Bool:
                if (!bool.TryParse(stringValue, out _))
                    ctx.AddFailure(field.Key, $"Field '{field.Key}' must be a boolean value.");
                break;

            case FieldTypeEnum.Date:
                if (!DateTime.TryParse(stringValue, out _))
                    ctx.AddFailure(field.Key, $"Field '{field.Key}' must be a valid date.");
                break;
        }

        // Проверка регулярного выражения
        if (!string.IsNullOrWhiteSpace(field.Regex) && stringValue is not null)
            if (!Regex.IsMatch(stringValue, field.Regex))
                ctx.AddFailure(field.Key, $"Field '{field.Key}' does not match the required format.");
    }

    private static void AddCrossFieldValidation(InlineValidator<IDictionary<string, object?>> validator, IFormSchemaRule rule)
    {
        switch (rule)
        {
            case EqualFieldsRule eq:
                validator.RuleFor(d => d).Custom((map, ctx) =>
                {
                    ValidateEqualFields(map, eq.Left, eq.Right, eq.Message, ctx);
                });
                break;

            case NotEqualFieldsRule ne:
                validator.RuleFor(d => d).Custom((map, ctx) =>
                {
                    ValidateNotEqualFields(map, ne.Left, ne.Right, ne.Message, ctx);
                });
                break;

            case OneOfRule one:
                validator.RuleFor(d => d).Custom((map, ctx) =>
                {
                    ValidateOneOf(map, one.Name, one.Allowed, one.Message, ctx);
                });
                break;

            case RequiredIfRule reqIf:
                validator.RuleFor(d => d).Custom((map, ctx) =>
                {
                    ValidateRequiredIf(map, reqIf, ctx);
                });
                break;
        }
    }

    private static void ValidateEqualFields(IDictionary<string, object?> map, string left, string right, string? message, ValidationContext<IDictionary<string, object?>> ctx)
    {
        map.TryGetValue(left, out var lv);
        map.TryGetValue(right, out var rv);

        // оба пустые — ок, не считаем ошибкой
        if (IsEmpty(lv) && IsEmpty(rv)) return;

        // null == "" → равны
        var ls = lv?.ToString() ?? string.Empty;
        var rs = rv?.ToString() ?? string.Empty;
        if (!string.Equals(ls, rs, StringComparison.Ordinal))
            ctx.AddFailure(right, message ?? $"Field '{right}' must equal '{left}'.");
    }

    private static void ValidateNotEqualFields(IDictionary<string, object?> map, string left, string right, string? message, ValidationContext<IDictionary<string, object?>> ctx)
    {
        map.TryGetValue(left, out var lv);
        map.TryGetValue(right, out var rv);
        var ls = lv?.ToString() ?? string.Empty;
        var rs = rv?.ToString() ?? string.Empty;
        if (string.Equals(ls, rs, StringComparison.Ordinal))
            ctx.AddFailure(right, message ?? $"Field '{right}' must NOT equal '{left}'.");
    }

    private static void ValidateOneOf(IDictionary<string, object?> map, string fieldName, IEnumerable<string> allowed, string? message, ValidationContext<IDictionary<string, object?>> ctx)
    {
        if (!map.TryGetValue(fieldName, out var val) || val is null)
            return;

        var s = val.ToString() ?? string.Empty;
        if (!allowed.Contains(s, StringComparer.Ordinal))
            ctx.AddFailure(fieldName, message ?? $"Field '{fieldName}' must be one of: {string.Join(", ", allowed)}.");
    }

    private static void ValidateRequiredIf(IDictionary<string, object?> map, RequiredIfRule rule, ValidationContext<IDictionary<string, object?>> ctx)
    {
        map.TryGetValue(rule.When.Field, out var wv);
        var ws = wv?.ToString();

        bool condition;
        if (rule.When.Value is null)
        {
            // Режим 1: equals не задан → "обязателен, если поле НЕ пустое"
            condition = !IsEmpty(ws);
        }
        else if (rule.When.Value.Length == 0)
        {
            // Режим 2: equals == "" → "обязателен, если поле ПУСТОЕ" (null/""/whitespace/отсутствует)
            condition = IsEmpty(ws);
        }
        else
        {
            // Режим 3: equals == "значение" → точное сравнение строк
            var expected = rule.When.Value;
            var actual   = ws ?? string.Empty;
            condition = string.Equals(actual, expected, StringComparison.Ordinal);
        }

        if (!condition) return;

        var name = rule.Then.Name;
        var isReq = rule.Then.Required;

        if (isReq && (!map.TryGetValue(name, out var tv) || IsEmpty(tv)))
            ctx.AddFailure(name, rule.Message ?? $"Field '{name}' is required.");
    }

    private static bool IsEmpty(object? value)
        => value is string s && string.IsNullOrWhiteSpace(s) || value is null;

    private static bool LooksLikeEmail(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var atIndex = s.IndexOf('@');
        if (atIndex <= 0) return false; // @ должен быть не в начале
        return s.LastIndexOf('.') > atIndex; // точка должна быть после @
    }

    private static bool IsValidPhone(string? s)
        => !string.IsNullOrWhiteSpace(s) &&
           Regex.IsMatch(s, @"^\+?\d{8,20}$") &&
           s.Count(char.IsDigit) >= 7;
}
