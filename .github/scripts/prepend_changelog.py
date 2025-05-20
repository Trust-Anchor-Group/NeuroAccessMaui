#!/usr/bin/env python3
import re
import sys
from pathlib import Path
import argparse
"""
Automated changelog prepender for GitHub Actions / local testing.

This script:
  1. Reads a PR body file and extracts categorized changelog entries from
     a 'Changelog' section (e.g. '### Added', '### Fixed').
  2. Inserts these entries under the '## [Unreleased]' section in a
     Keep-A-Changelog formatted CHANGELOG.md, preserving historical
     entries.
  3. Normalizes Markdown spacing for headers and lists.

Usage:
  python prepend_changelog.py --pr-body-file pr_body.txt --changelog CHANGELOG.md
"""
import re
import sys
from pathlib import Path
from typing import Dict, List

def extract_categorized_changelog(pr_body: str) -> Dict[str, List[str]]:
    """
    Extract lists of changelog bullets categorized by subheading from a PR body.

    Scans for a top-level 'Changelog' heading (levels 2–4, with optional 🔄 emoji),
    then splits into subcategories by '### <Category>' lines.

    Args:
        pr_body: Full PR description text.

    Returns:
        A dict mapping category names (e.g. 'Added', 'Changed') to lists of
        bullet strings (without leading '- '). Empty dict if no section found.
    """
    HEADER_PATTERN = re.compile(
        r"^(?:#{2,4}\s*(?:🔄\s*)?Changelog)\s*$"  # '## Changelog' or similar
        r"[\r\n]+"
        r"([\s\S]*?)(?=^##\s*\[|\Z)",
        re.MULTILINE
    )
    SUBHEADER_PATTERN = re.compile(r"^###\s+(\w+)", re.MULTILINE)

    match = HEADER_PATTERN.search(pr_body)
    if not match:
        return {}

    block = match.group(1)
    categorized: Dict[str, List[str]] = {}
    current_cat: str = ''

    for line in block.splitlines():
        header_match = SUBHEADER_PATTERN.match(line)
        if header_match:
            current_cat = header_match.group(1)
            categorized.setdefault(current_cat, [])
        elif current_cat and line.strip().startswith('-'):
            # strip leading '-' and any whitespace
            categorized[current_cat].append(line.strip()[1:].strip())

    return categorized


def prepend_to_category(changelog_text: str, category: str, items: List[str]) -> str:
    """
    Inject bullet items under a given category in the 'Unreleased' section.

    - If the category exists, bullets are appended to it.
    - Otherwise, a new '### <Category>' block is added at the end of Unreleased.

    Args:
        changelog_text: Full contents of CHANGELOG.md.
        category: Subsection name under Unreleased (e.g. 'Added').
        items: List of bullet text (no dash).

    Returns:
        Updated changelog text.
    """
    # Capture Unreleased section block
    UNREL_PATTERN = re.compile(r"(##\s*\[Unreleased\][\s\S]*?)(?=^##\s*\[|\Z)", re.MULTILINE)
    m = UNREL_PATTERN.search(changelog_text)
    if not m:
        raise ValueError("'## [Unreleased]' section not found in CHANGELOG.md")

    unrel_block = m.group(1)
    # Format items as Markdown list lines
    bullets = '\n'.join(f"- {line}" for line in items)

    # Patterns to locate existing subheader
    subhdr_regex = re.compile(rf"^(###\s*{re.escape(category)}\s*)$", re.MULTILINE)

    if subhdr_regex.search(unrel_block):
        # Append under existing subheader
        def repl(match: re.Match) -> str:
            header_line = match.group(1).rstrip()
            return f"\n{header_line}\n\n{bullets}"  # blank lines ensure spacing

        new_unrel = subhdr_regex.sub(repl, unrel_block, count=1)
    else:
        # Create new subheader at end of Unreleased block
        trimmed = re.sub(r"[\r\n]+$", "", unrel_block)
        new_unrel = f"{trimmed}\n\n### {category}\n\n{bullets}\n"  # blank lines before header & list

    return changelog_text[:m.start(1)] + new_unrel + changelog_text[m.end(1):]


def normalize_markdown(text: str) -> str:
    """
    Normalize blank lines for headers and lists.

    - Collapse 3+ blank lines into 2.
    - Ensure blank line before and after headers (##, ###).
    - Ensure a blank line before list items.
    """
    # Collapse excessive blank lines
    text = re.sub(r"(\n[ \t]*){3,}", "\n\n", text)
    # Ensure blank line before headers
    text = re.sub(r"(?<!\n\n)(^##+\s.*$)", r"\n\1", text, flags=re.MULTILINE)
    # Ensure blank line after headers
    text = re.sub(r"(^##+\s.*$)(?!\n\n)", r"\1\n", text, flags=re.MULTILINE)

    return text


def parse_args() -> argparse.Namespace:
    """Parse command-line arguments."""
    import argparse
    parser = argparse.ArgumentParser(
        description="Update CHANGELOG.md with PR changelog entries under Unreleased"
    )
    parser.add_argument(
        '--pr-body-file',
        required=True,
        #default='test_pr_body.txt',
        help='Path to a text file containing the PR body'
    )
    parser.add_argument(
        '--changelog',
        default='CHANGELOG.md',
        help='Path to the changelog file'
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    pr_path = Path(args.pr_body_file)
    log_path = Path(args.changelog)

    if not pr_path.exists() or not log_path.exists():
        sys.exit("ERROR: file(s) not found: %s, %s" % (pr_path, log_path))

    pr_body = pr_path.read_text(encoding='utf-8')
    categorized = extract_categorized_changelog(pr_body)
    if not categorized:
        print("No changelog entries found in PR body.")
        return

    changelog = log_path.read_text(encoding='utf-8')
    for category, items in categorized.items():
        changelog = prepend_to_category(changelog, category, items)
    changelog = normalize_markdown(changelog)
    log_path.write_text(changelog, encoding='utf-8')

    print(f"✅ Updated CHANGELOG.md with: {', '.join(categorized.keys())}")


if __name__ == '__main__':
    main()
