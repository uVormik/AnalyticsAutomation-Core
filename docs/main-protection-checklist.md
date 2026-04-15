# Main Protection Checklist

## Назначение
Этот документ фиксирует минимальную защиту для `main` на Sprint 0.

## Перед включением protection
- `main` существует и назначена default branch.
- README и ADR baseline уже лежат в `main`.
- `.github/CODEOWNERS`, PR template и issue templates уже лежат в `main`.
- placeholder-ы в `CODEOWNERS` заменены на реальные GitHub users/teams с write access.
- merge strategy на уровне репозитория уже зафиксирована как squash-first.

## Где настраивать
Использовать один из двух вариантов:
- branch ruleset targeting `main`
- classic branch protection rule for `main`

## Включить сразу в S0-02
- Require a pull request before merging
  - Required approvals = 1
  - Dismiss stale pull request approvals when new commits are pushed
  - Require review from Code Owners
  - Require approval of the most recent reviewable push
- Require conversation resolution before merging
- Require linear history
- Do not allow bypassing the above settings
- Allow force pushes = off
- Allow deletions = off

## Зафиксировать текстом сейчас, включить в S0-04
- Require status checks to pass before merging
- Require branches to be up to date before merging
- Required checks names:
  - `ci / restore`
  - `ci / build`
  - `ci / unit-tests`
  - `ci / integration-tests`
  - `ci / format`

## Что не включать на S0-02
- Require deployments to succeed before merging
- Require merge queue
- Require signed commits
- Lock branch

## Проверка после настройки
1. direct push в `main` невозможен
2. PR в `main` не мерджится без approval
3. PR по paths из `CODEOWNERS` запрашивает корректный review
4. после S0-04 PR не мерджится без required checks