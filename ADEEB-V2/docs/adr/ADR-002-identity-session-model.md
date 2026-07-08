# ADR-002 Identity Session Model

Identity uses first-class refresh sessions instead of a single refresh token per user. This supports multi-device login, rotation, token-family reuse detection, and targeted revocation.
