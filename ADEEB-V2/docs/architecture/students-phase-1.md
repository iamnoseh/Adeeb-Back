# Students Phase 1

Students are first-class personas with independent `StudentId` values. A student links to Identity through immutable `IdentityUserId`; Identity remains the authority for authentication and first/last name.

Student provisioning is idempotent. Public registration creates the Identity user first, then provisions the Student persona through the Students module. This is not a distributed transaction. If Student provisioning fails after Identity creation, the Identity account remains and provisioning can be retried through `POST /api/v2/students/me/provision`.

Phase 1 intentionally does not model premium, commerce, payments, subscriptions, gamification, attempts, referrals, mentor personas, or social login.

The Students module owns the `students` PostgreSQL schema. It does not create database foreign keys to Identity-owned tables.
