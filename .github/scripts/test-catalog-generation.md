# How to Test the Project Catalog Generation

## Option 1: Run Workflow Manually via GitHub Actions UI

1. Go to your repository on GitHub
2. Click on **Actions** tab
3. Find **"Update Project Catalog"** workflow in the left sidebar
4. Click **"Run workflow"** button (top right)
5. Select your current branch
6. Click **"Run workflow"** button
7. Wait for the workflow to complete
8. Check the generated `Ivy-All-Project.json` file in the repository root

## Option 2: Run Workflow via GitHub CLI

```bash
# Make sure you're authenticated
gh auth login

# Get your current branch
BRANCH=$(git branch --show-current)

# Run the workflow
gh workflow run "Update Project Catalog.yml" --ref $BRANCH

# Watch the workflow run
gh run watch
```

## Option 3: Test Script Syntax (if you have WSL or Linux)

If you have WSL (Windows Subsystem for Linux) or access to a Linux machine:

```bash
# Check script syntax
bash -n .github/scripts/generate-project-catalog.sh

# If you have jq installed, you can test the script
cd /path/to/repo
bash .github/scripts/generate-project-catalog.sh
```

## Option 4: Review the Generated File

After the workflow runs, you can:
1. Check the `Ivy-All-Project.json` file in the repository root
2. Verify it has the correct structure:
   - `project-demos` array
   - `package-demos` array
   - Each entry has: name, description, githubLink, deploymentLink, tags

## Quick Verification Checklist

- [ ] Workflow file exists: `.github/workflows/update-project-catalog.yml`
- [ ] Script file exists: `.github/scripts/generate-project-catalog.sh`
- [ ] Script has execute permissions (set by workflow)
- [ ] Workflow has `workflow_dispatch` trigger (for manual runs)
- [ ] Workflow has daily schedule (cron: `0 2 * * *`)

