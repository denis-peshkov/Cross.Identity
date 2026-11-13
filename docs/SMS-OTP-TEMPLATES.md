# SMS OTP templates

Email OTP keeps the long `verify.*` / `reset.*` templates.

SMS uses short single-segment templates:

| Channel | Template name | Example file |
| --- | --- | --- |
| Email | `verify`, `reset` | `verify.en.txt` |
| SMS | `verify-sms`, `reset-sms` | `verify-sms.en.txt` |

`SendCodeStep` appends `-sms` automatically when the OTP channel is SMS.

## Segment limits (SMSClub)

| Parts | Latin | Cyrillic |
| --- | ---: | ---: |
| 1 | 160 | 70 |
| 2 | 306 | 134 |
| 3 | 459 | 201 |
| 4 | 612 | 268 |

Max **4 parts**; prefer **≤ 3**. OTP should stay in **1 segment**.

Current SMS bodies (after `{{code}}` substitution, ~6 digits) stay well under one segment even with Cyrillic.
