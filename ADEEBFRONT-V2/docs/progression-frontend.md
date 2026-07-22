# Progression frontend

## Admin

The league administration page is available at `/admin/progression`. It supports league creation and cosmetic editing, avatar preview/removal, archival outside an active season, first-season start, automatic-renewal control, current leaderboards, and completed-season history.

Thresholds use inclusive minimum and exclusive maximum values. The highest league has no maximum. Structural controls are disabled while a season is active; the API remains authoritative and returns localized problem details for invalid ranges.

## Student

The student league page is available at `/student/league`. It renders the current localized league, cycle XP, league rank, lifetime rank, server-synchronized countdown, movement zones, podium, paginated leaderboard, the student's highlighted row, and the previous season result.

The countdown derives its clock offset from `serverNowUtc`; when it reaches zero the overview and leaderboard are refreshed. XP, ranks, and movement zones are never calculated in the browser.

The student profile also exposes lifetime XP, global rank, current league, and a link to the league page.
