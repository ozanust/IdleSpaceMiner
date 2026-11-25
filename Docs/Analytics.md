# Analytics

Firebase Analytics (com.google.firebase.analytics 14.1.0, from `Packages/Firebase`).
All game events go through `IAnalyticsService` (`Assets/Scripts/Analytics`), so swapping backend means one new adapter.
In the editor and development builds every event is also printed to the console as `[Analytics] name params`.

## Setup still needed in Firebase

1. Create a Firebase project and add the Android app (package name from Player Settings) and the iOS app.
2. Put `google-services.json` and `GoogleService-Info.plist` anywhere under `Assets/`.
3. Android: Assets > External Dependency Manager > Android Resolver > Force Resolve.
4. To watch events live: enable DebugView (`adb shell setprop debug.firebase.analytics.app <package>`), then Firebase console > DebugView.

Without the config files the game runs normally and simply sends nothing.

## Where to read what

| Question | Where in Firebase |
|---|---|
| Retention (D1, D7, D30) | Analytics > Retention (cohorts by `first_open`, collected automatically) |
| Daily / monthly active users, stickiness | Analytics > Dashboard, Engagement |
| Average engagement (play) time per user, per day | Engagement > Overview (`user_engagement`, automatic) |
| Sessions per user, session length | `session_start` (automatic); our `session_end.session_seconds` for exact length |
| Return days in a custom funnel | `day_active` with `day` = days since install (0, 1, 2, 7 ...) |
| Total play time per player | `play_time_reached` milestones, user property `play_minutes` |
| Tutorial drop-off | Funnel: `tutorial_begin` > `tutorial_step` (step 1 to the last step) > `tutorial_complete` |
| Progress pacing | `planet_unlocked`, `research_completed`, `level_up` (level = planets owned), each with `play_minutes` and `day` |
| Prestige loop | `galaxy_sold` (reward, galaxy value, minutes in that galaxy), `room_upgraded` |
| Economy | `earn_virtual_currency` / `spend_virtual_currency` (Dark Matter by source and item, money on planets and machines), `resources_sold`, `purchase_failed` |
| Ads | `ad_requested`, `ad_rewarded`, `ad_closed_early`, `ad_unavailable` by `placement` (mine_boost, ark) |

Register the custom parameters you want in reports under Analytics > Custom definitions
(for example `play_minutes`, `day`, `session_seconds`, `planets`, `galaxy`, `placement`, `step`).

## Events

Sessions and retention (AnalyticsController):
- `session_begin`: session_number, day, active_days, hours_away, play_minutes. A new session starts at launch or after 5+ minutes in the background.
- `session_end`: reason (background, quit), session_number, session_seconds, play_minutes, upgrades, upgrade_spend, sales_income, asteroids, planets, galaxy.
- `day_active`: day, active_days, play_minutes. Once per calendar day.
- `play_time_reached`: minutes (5, 15, 30, 60, 120, 240, 480, 1000, 2000), day, planets, galaxy.

Progress:
- `planet_unlocked`: planet_id, planet_name, planets, galaxy, play_minutes, session_minutes, day. Also `level_up` (level = planets owned).
- `research_completed`: research, research_count (+ progress fields). Also `unlock_achievement`.
- `machine_unlocked`: machine (smelter, crafter), number. `recipe_unlocked`: recipe.
- `galaxy_sold`: reward_dark_matter, galaxy_value, galaxies_sold, galaxy_play_minutes, planets, research_count.
- `asteroid_mined`: session_count.
- `tutorial_begin`, `tutorial_step` (step, steps), `tutorial_complete` (skipped, step).
- `task_claimed`: task_index, task_type, reward, claimed_total. `achievement_claimed`: achievement_index, goal_type, target, claimed_total.

Economy:
- `resources_sold`: resource, category, amount, value.
- `spend_virtual_currency`: virtual_currency_name (money, dark_matter), value, item_name.
- `earn_virtual_currency`: virtual_currency_name (dark_matter), value, source (galaxy_sale, achievement, ark).
- `purchase_failed`: kind (Planet, Upgrade, Production, Manager, Room).
- `manager_hired` (school, stars, total_hired), `manager_slot_bought` (slots), `manager_promoted` (stars), `room_upgraded` (room, level, cost, galaxies_sold).
- `ark_cash`: value. `offline_progress`: minutes_away, minutes_credited, capped.

Ads: `ad_requested`, `ad_rewarded`, `ad_closed_early`, `ad_unavailable`, each with placement.

Other: `screen_view` (screen_name = window), `progress_reset`.

## User properties

install_version, sessions, days_since_install, play_minutes, planets_owned, research_count, galaxies_sold.
Use them as comparison filters, for example retention of players who sold a galaxy against those who didn't.

## Privacy and consent

`ConsentService` (Google's User Messaging Platform, bundled with the AdMob plugin) runs at launch:
- It checks where the player is and shows Google's consent form where the law requires it (EEA, UK, Switzerland).
- Ads start only after that (`ConsentInformation.CanRequestAds`).
- Firebase starts with every consent type denied (`Assets/Plugins/Android/ConsentDefaults.androidlib`) and gets
  analytics, ad storage, ad user data and ad personalisation from the player's answers (Google consent mode).
  Without analytics consent Firebase only sends cookieless pings.
- Settings shows a "Privacy settings" button where players must be able to change their answer.

For the form to appear, create a GDPR message in AdMob > Privacy & messaging for the app. Test it from the EEA,
or with `ConsentDebugSettings.DebugGeography = DebugGeography.EEA` and your device's test ID.
Also add the data use to the Google Play data safety form and the App Store privacy details.
