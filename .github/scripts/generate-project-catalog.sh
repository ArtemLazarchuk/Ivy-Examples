#!/bin/bash

# Script to generate Ivy-All-Projects.json with information about all projects and demos

set -e

REPO_URL="https://github.com/Ivy-Interactive/Ivy-Examples"
OUTPUT_FILE="Ivy-All-Projects.json"

# Function to extract project name from README.md
extract_project_name() {
    local readme_file="$1"
    local folder_name="$2"
    
    if [ -f "$readme_file" ]; then
        # Extract first # heading, remove # and trim
        local title=$(grep -m 1 "^# " "$readme_file" 2>/dev/null | sed 's/^# //' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
        if [ -n "$title" ] && [ "$title" != "" ]; then
            echo "$title"
            return
        fi
    fi
    
    # Fallback to folder name, capitalize first letter
    echo "$folder_name" | awk '{print toupper(substr($0,1,1)) substr($0,2)}'
}

# Function to extract description from README.md
extract_description() {
    local readme_file="$1"
    
    if [ ! -f "$readme_file" ]; then
        echo ""
        return
    fi
    
    # First, try to find "Description" heading and extract text after it
    local in_code_block=false
    local found_description=false
    local description=""
    
    while IFS= read -r line || [ -n "$line" ]; do
        # Skip code blocks
        if [[ "$line" =~ ^\`\`\` ]]; then
            in_code_block=$([ "$in_code_block" = false ] && echo true || echo false)
            continue
        fi
        [ "$in_code_block" = true ] && continue
        
        # Look for Description heading (case-insensitive, supports # or ##)
        # Use case-insensitive matching
        if echo "$line" | grep -qiE '^#+\s+Description'; then
            found_description=true
            continue
        fi
        
        # If we found Description heading, collect text until next heading
        if [ "$found_description" = true ]; then
            # Stop at next heading (but not if it's still Description with different case)
            if echo "$line" | grep -qE '^#+\s+' && ! echo "$line" | grep -qiE '^#+\s+Description'; then
                break
            fi
            
            # Skip empty lines, images, badges
            if [[ -z "${line// }" ]] || \
               [[ "$line" =~ ^!\[.*\] ]] || \
               [[ "$line" =~ ^\[!\[.*\] ]] || \
               [[ "$line" =~ ^\<img ]] || \
               [[ "$line" =~ ^\[Open\ in ]]; then
                continue
            fi
            
            # Remove markdown formatting but keep text
            line=$(echo "$line" | sed 's/\[\([^]]*\)\]([^)]*)/\1/g' | sed 's/\*\*//g' | sed 's/\*//g' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
            
            if [ -n "$line" ]; then
                if [ ${#description} -gt 0 ]; then
                    description="$description $line"
                else
                    description="$line"
                fi
            fi
        fi
    done < "$readme_file"
    
    # If Description heading was not found, return empty string
    if [ "$found_description" = false ]; then
        echo ""
        return
    fi
    
    # Clean up description
    description=$(echo "$description" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//' | sed 's/[[:space:]]\+/ /g')
    
    # Return the description (even if empty, if Description section exists but is empty)
    echo "$description"
}

# Function to extract packages from .csproj
extract_packages() {
    local csproj_file="$1"
    local packages=()
    
    if [ ! -f "$csproj_file" ]; then
        echo "[]"
        return
    fi
    
    # Extract PackageReference, exclude Ivy
    # Use sed to extract package names from PackageReference lines
    while IFS= read -r package; do
        if [ -n "$package" ] && [ "$package" != "Ivy" ]; then
            packages+=("$package")
        fi
    done < <(grep 'PackageReference Include=' "$csproj_file" | sed -n 's/.*Include="\([^"]*\)".*/\1/p' | grep -v "^Ivy$" || true)
    
    # Convert to JSON array
    if [ ${#packages[@]} -eq 0 ]; then
        echo "[]"
    else
        printf '%s\n' "${packages[@]}" | jq -R . | jq -s .
    fi
}

# Function to generate tags
generate_tags() {
    local packages_json="$1"
    local project_name="$2"
    local folder_name="$3"
    
    local tags=()
    
    # Add packages as tags
    if [ "$packages_json" != "[]" ] && [ -n "$packages_json" ]; then
        while IFS= read -r package; do
            tags+=("$package")
        done < <(echo "$packages_json" | jq -r '.[]')
    fi
    
    # Infer category from package name or project name
    local name_lower=$(echo "$project_name" | tr '[:upper:]' '[:lower:]')
    local folder_lower=$(echo "$folder_name" | tr '[:upper:]' '[:lower:]')
    
    # Add category tags based on common patterns
    if [[ "$name_lower" =~ pdf|questpdf ]]; then
        tags+=("PDF")
    elif [[ "$name_lower" =~ qr|qrcoder ]]; then
        tags+=("QR Code")
    elif [[ "$name_lower" =~ excel|closedxml|epplus|miniexcel ]]; then
        tags+=("Excel")
    elif [[ "$name_lower" =~ csv|csvhelper ]]; then
        tags+=("CSV")
    elif [[ "$name_lower" =~ json|newtonsoft ]]; then
        tags+=("JSON")
    elif [[ "$name_lower" =~ image|magick|barcode ]]; then
        tags+=("Image Processing")
    elif [[ "$name_lower" =~ ai|openai|ollama|semantickernel ]]; then
        tags+=("AI")
    elif [[ "$name_lower" =~ date|time|datetime|cronos|nodatime ]]; then
        tags+=("Date/Time")
    elif [[ "$name_lower" =~ auth|jwt|stripe ]]; then
        tags+=("Security")
    fi
    
    # Add demo tag
    tags+=("demo")
    
    # Convert to JSON array
    if [ ${#tags[@]} -eq 0 ]; then
        echo "[]"
    else
        printf '%s\n' "${tags[@]}" | jq -R . | jq -s .
    fi
}

# Function to process a project
process_project() {
    local project_dir="$1"
    local folder_type="$2"  # "packages-demos" or "project-demos"
    local folder_name=$(basename "$project_dir")
    
    local readme_file="$project_dir/README.md"
    local csproj_file=$(find "$project_dir" -maxdepth 1 -name "*.csproj" | head -1)
    
    if [ -z "$csproj_file" ]; then
        echo "Warning: No .csproj found in $project_dir" >&2
        return
    fi
    
    # Extract data
    local project_name=$(extract_project_name "$readme_file" "$folder_name")
    local description=$(extract_description "$readme_file")
    local packages_json=$(extract_packages "$csproj_file")
    local tags_json=$(generate_tags "$packages_json" "$project_name" "$folder_name")
    
    # Generate links
    local github_link="$REPO_URL/tree/main/$folder_type/$folder_name"
    # Convert folder_type to deployment prefix: "packages-demos" -> "packagedemos", "project-demos" -> "projectdemos"
    local folder_prefix
    if [ "$folder_type" = "packages-demos" ]; then
        folder_prefix="packagedemos"
    elif [ "$folder_type" = "project-demos" ]; then
        folder_prefix="projectdemos"
    else
        folder_prefix=$(echo "$folder_type" | sed 's/-//g')
    fi
    local deployment_link="ivy-${folder_prefix}-${folder_name}.sliplane.app"
    
    # Create JSON object
    jq -n \
        --arg name "$project_name" \
        --arg desc "$description" \
        --arg github "$github_link" \
        --arg deploy "$deployment_link" \
        --argjson tags "$tags_json" \
        '{
            name: $name,
            description: $desc,
            githubLink: $github,
            deploymentLink: $deploy,
            tags: $tags
        }'
}

# Main execution
echo "🔍 Scanning projects..."

# Initialize JSON structure
project_demos=()
package_demos=()

# Process project-demos
if [ -d "project-demos" ]; then
    echo "📁 Processing project-demos..."
    while IFS= read -r project_dir; do
        if [ -d "$project_dir" ]; then
            project_json=$(process_project "$project_dir" "project-demos")
            if [ -n "$project_json" ]; then
                project_demos+=("$project_json")
            fi
        fi
    done < <(find project-demos -maxdepth 1 -type d ! -path project-demos | sort)
fi

# Process packages-demos
if [ -d "packages-demos" ]; then
    echo "📦 Processing packages-demos..."
    while IFS= read -r project_dir; do
        if [ -d "$project_dir" ]; then
            project_json=$(process_project "$project_dir" "packages-demos")
            if [ -n "$project_json" ]; then
                package_demos+=("$project_json")
            fi
        fi
    done < <(find packages-demos -maxdepth 1 -type d ! -path packages-demos | sort)
fi

# Combine into final JSON
echo "📝 Generating JSON..."

# Convert arrays to JSON
project_demos_json=$(printf '%s\n' "${project_demos[@]}" | jq -s .)
package_demos_json=$(printf '%s\n' "${package_demos[@]}" | jq -s .)

# Create final JSON structure
jq -n \
    --argjson project_demos "$project_demos_json" \
    --argjson package_demos "$package_demos_json" \
    '{
        "project-demos": $project_demos,
        "package-demos": $package_demos
    }' > "$OUTPUT_FILE"

echo "✅ Generated $OUTPUT_FILE"
echo "   - Project demos: $(echo "$project_demos_json" | jq 'length')"
echo "   - Package demos: $(echo "$package_demos_json" | jq 'length')"

