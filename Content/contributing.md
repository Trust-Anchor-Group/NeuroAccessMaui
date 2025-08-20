# Contributing to Neuro-Access

This guide outlines the process for contributing, the branching strategy, coding guidelines, and other important aspects to ensure that your contributions are as smooth and impactful as possible.

## Table of Contents

- [Branching Strategy](#branching-strategy)
- [How to Contribute](#how-to-contribute)
- [Coding Guidelines](#coding-guidelines)
- [Issue Reporting](#issue-reporting)

## Branching Strategy

We use a **Git Flow** branching strategy to manage the development process efficiently.

### Key Branches

- **`main`**: The main branch contains stable, production-ready code.
- **`dev`**: This branch contains code that is ready for testing but not yet released. It is the integration branch for features.
- **`feature/*`**: Feature branches are used to develop new features. These branches branch off from `dev` and are merged back into `dev` once completed.
- **`fix/*`** or **`bugfix/*`**: Used for fixing bugs discovered during development. Prefer the existing convention in the repository (you may see both). Branch off from `dev` and merge back into `dev`.
- **`hotfix/*`**: For urgent fixes in production. These branch off from `main` and should be merged into both `main` and `dev`.
- **`release/*`**: Used to prepare for a new production release. These branch of `dev` and allows for last-minute adjustments before merging into `main`.

### Workflow

1. Create a new branch from `dev` for your feature or bugfix (ensure `dev` is up to date with upstream first).
2. Work on your branch, commit regularly, and push your changes.
3. Create a pull request (PR) against `dev` once your work is complete.
4. Your PR will be reviewed, and after approval, it will be merged into `dev`.
5. Merge `dev` into `main` when ready for a new release.

## How to Contribute

### Setting Up Your Development Environment

1. **Fork the Repository**: Create your fork of the project repository on GitHub.
2. **Clone Your Fork**: Clone your fork to your local machine:
   ```bash
   git clone https://github.com/your-username/NeuroAccessMaui.git
   ```
3. **Set Up Upstream**: Set the original repository as the upstream to keep your fork updated:
   ```bash
   git remote add upstream https://github.com/trust-anchor-group/NeuroAccessMaui.git
   ```

### Submitting a Pull Request

1. **Sync with Upstream**: Before creating a new branch, ensure your fork is up-to-date with the upstream `dev` branch:
   ```bash
   git checkout dev
   git pull upstream dev
   ```
2. **Create a Branch**: Use descriptive branch names that reflect the work being done:
   ```bash
   git checkout -b feature/awesome-feature
   ```
3. **Make Your Changes**: Commit changes frequently with clear and concise commit messages.
4. **Push Your Branch**: Push your changes to your fork:
   ```bash
   git push origin feature/awesome-feature
   ```
5. **Open a Pull Request**: Go to GitHub and open a pull request against the `dev` branch of the main repository.

## Coding Guidelines

To maintain consistency and quality across the codebase, please follow these guidelines:

### Style Guide
- Use **PascalCase** for function and method names.
- Use **PascalCase** for class and interface names.
- Use **PascalCase** for public properties.
- Use **camelCase** for private variables.
- Always include XML documentation for public methods and classes.
- Try to use meaningful names for variables, methods, and classes that clearly express their intent.

### Commit Messages

- Keep commit messages short but descriptive. Include additional context in the body if necessary.

## Issue Reporting

We appreciate your feedback and help in making Neuro-Access better. To report an issue, please follow these steps:

1. **Check for Existing Issues**: Before creating a new issue, check if it has already been reported.
2. **Open a New Issue**: If the issue is new, [open an issue](https://github.com/NeuroAccess/NeuroAccessMaui/issues/new) with a descriptive title and detailed information.
3. **Provide Details**: Include as much information as possible, such as:
   - Steps to reproduce the issue.
   - Expected and actual behavior.
   - Screenshots or error messages if applicable.
   - Environment details (Platform, .NET version, etc.).
   - Proposed solution - if any.
## Thank You!

We value your contributions and feedback. Together, we can make Neuro-Access even better!

---