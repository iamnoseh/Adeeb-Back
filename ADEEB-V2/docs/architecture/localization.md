# Localization

Supported cultures are `tg-TJ`, `ru-RU`, and `en-US`; the default is `tg-TJ`.

Request culture precedence is:

1. Authenticated user's stored preferred language, when the request is authenticated.
2. `X-Adeeb-Language`.
3. `Accept-Language`.
4. `tg-TJ`.

Backend localization is for API errors, validation messages, and system-generated text. Flutter owns UI localization. Future database business content should use translation tables, for example `Mission` and `MissionTranslation`.

Operational timestamps are persisted in UTC. API responses may include Dushanbe presentation fields with `+05:00` offset, for example `accessTokenExpiresAtDushanbe`, so Swagger and web/mobile clients can display Tajikistan local time without changing persisted UTC values.
