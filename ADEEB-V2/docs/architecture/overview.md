# ADEEB V2 Architecture Overview

ADEEB V2 starts as a modular monolith. The API host composes building blocks and modules, while each module owns its domain, application behavior, endpoints, and persistence mapping.

Phase 0 and Phase 1 implement only the backend foundation and Identity module. Business modules such as tests, missions, payments, gamification, and AI mentor are intentionally not implemented.
