# Contributing to Kaz.Http

Thank you for your interest in contributing! Here is everything you need to get started.

---

## Getting Started

1. Fork the repository
2. Clone your fork
```bash
git clone https://github.com/your-username/Kaz.Http.git
```
3. Create a branch for your changes
```bash
git checkout -b feature/your-feature-name
```

---

## How to Contribute

<details>
<summary><b>Reporting Bugs</b></summary>

Open an issue on GitHub and include:
- A clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- .NET version and OS

</details>

<details>
<summary><b>Suggesting Features</b></summary>

Open an issue with the `enhancement` label and describe:
- What problem it solves
- How you expect the API to look
- Any relevant examples

</details>

<details>
<summary><b>Submitting a Pull Request</b></summary>

1. Make sure your changes compile without errors or warnings
2. Follow the existing code style
3. Add XML documentation to any new public members
4. Open a pull request against the `main` branch with a clear description of what you changed and why

</details>

---

## Code Style

- Use `camelCase` for private fields with `_` prefix — e.g. `_myField`
- Use `PascalCase` for public members
- Keep methods focused and short
- XML documentation is required for all public members
- Avoid unnecessary comments — code should be self-explanatory

---

## Commit Messages

Keep commit messages short and descriptive:

```
Add bulkhead isolation support
Fix circuit breaker state not resetting after recovery
Update retry delay documentation
```

---

## Questions

If you have any questions feel free to reach out:


[![LinkedIn](https://img.shields.io/badge/LinkedIn-white?logo=data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiIHN0YW5kYWxvbmU9Im5vIj8+Cjxzdmcgd2lkdGg9IjE4LjgwOTIyM21tIiBoZWlnaHQ9IjE5LjAwMDAwNG1tIiB2aWV3Qm94PSIwIDAgMTguODA5MjIzIDE5LjAwMDAwNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPgogIDxnIHRyYW5zZm9ybT0idHJhbnNsYXRlKC0zNC4wMTgzMjgsLTY5LjkyOTc1MikiPgogICAgPGc+CiAgICAgIDxyZWN0IHN0eWxlPSJmaWxsOiMwMDc3YjU7ZmlsbC1vcGFjaXR5OjEiIHdpZHRoPSIxOC44MDkyMjUiIGhlaWdodD0iMTkuMDAwMDA2IiB4PSIzNC4wMTgzMjYiIHk9IjY5LjkyOTc0OSIgcnk9IjEuODkwNTQ3OSIvPgogICAgICA8ZWxsaXBzZSBzdHlsZT0iZmlsbDojZmZmZmZmO2ZpbGwtb3BhY2l0eToxIiBjeD0iMzguMDkwMjM3IiBjeT0iNzMuMDY4NTUiIHJ4PSIxLjQyNzg3OTgiIHJ5PSIxLjQzNTMyMzUiLz4KICAgICAgPHBhdGggc3R5bGU9ImZpbGw6I2ZmZmZmZjtmaWxsLW9wYWNpdHk6MSIgZD0ibSAzNi41NDA2ODcsNzUuNjg5NzYyIHYgMTAuNzU3NTc2IGggMi45Nzc0MyBWIDc1LjY4OTc2MiBaIi8+CiAgICAgIDxnIHRyYW5zZm9ybT0idHJhbnNsYXRlKC0xLjIzNTgxMjIsMC4xMTI2MjczNykiPgogICAgICAgIDxwYXRoIHN0eWxlPSJmaWxsOiNmZmZmZmY7ZmlsbC1vcGFjaXR5OjEiIGQ9Im0gNDIuOTIxOTE1LDg2LjI1NzU3NCBoIDIuOTc3NDMgViA3NS40OTk5OTggaCAtMi45Nzc0MyB6Ii8+CiAgICAgICAgPHBhdGggc3R5bGU9ImZpbGw6I2ZmZmZmZjtmaWxsLW9wYWNpdHk6MSIgZD0ibSA0OC43MDYyMTQsODYuMjU3NTc0IHYgLTguODAxNjUyIGwgMy4xNDc5OSwxMGUtNyAzZS02LDguODAxNjUxIHoiLz4KICAgICAgICA8cGF0aCBzdHlsZT0iZmlsbDojZmZmZmZmO2ZpbGwtb3BhY2l0eToxIiBkPSJtIDUxLjM3OTczOCw4Ny45NTI0MzYgYyAtMC41NzQzMjgsLTAuMDQ3NTYgLTAuODYxNDkzLC0wLjY0MjY4NCAtMS4xNDg2NTcsLTEuMjM3ODA3IDAuMjg4MTM5LC0wLjQwNDg3NyAwLjU3NjI3MiwtMC44MDk3NDYgMS40NDIwNDcsLTEuMTgxNTQ5IDAuODY1Nzc0LC0wLjM3MTgwNCAyLjMwOTA5MywtMC43MTA1MDYgMy42NTM1NDksLTAuNjU0ODIzIDEuMzQ0NDU3LDAuMDU1NjggMi41ODk4OTUsMC41MDU3NDQgMy4xNjQ4NDQsMS4yNTg0NDkgMC41NzQ5NDgsMC43NTI3MDUgMC40NzkzNCwxLjgwNzk4OCAtMC40MzQ4MzEsMi4wMDIzMDYgLTAuOTE0MTcyLDAuMTk0MzE5IC0yLjY0Njg3MywtMC40NzIzMzUgLTMuOTQ0MDEsLTAuNTU1NjcgLTEuMjk3MTM4LC0wLjA4MzM0IC0yLjE1ODYxNCwwLjQxNjY1NiAtMi43MzI5NDIsMC4zNjkwOTQgeiIgdHJhbnNmb3JtPSJtYXRyaXgoMC44NjQwMzI0NiwwLDAsMC45Nzc5NjE0MywxLjAwOTM0NTMsLTcuNjI2NzE5OCkiLz4KICAgICAgPC9nPgogICAgPC9nPgogIDwvZz4KPC9zdmc+Cg==)](https://linkedin.com/in/artem-kazantsev-39a3213b9)
