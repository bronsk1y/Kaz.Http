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

[![LinkedIn](https://img.shields.io/badge/LinkedIn-%230077B5.svg?logo=linkedin&logoColor=white)](https://linkedin.com/in/artem-kazantsev-39a3213b9)
