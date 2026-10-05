#!/bin/sh
# Forces the scratch host to recompile (junction edits are not always picked up) and prints errors.
cd "$(dirname "$0")"
python mcp.py call editor_stop '{}' >/dev/null 2>&1
python mcp.py call code_write_file "{\"path\":\"Code/RebuildTrigger.cs\",\"content\":\"// rebuild $(date +%s%N)\n\"}" >/dev/null
python mcp.py call compile_await '{"timeoutSeconds":240}' | grep -E '"clean"|"hotSwapped"|"errorCount"'
python mcp.py call code_get_compile_errors '{}' | grep -o '"message": "[^"]*"' | grep -v "Unable to resolve" | head -40
