#!/bin/sh
# Enters play mode in the scratch editor, runs the in-engine self-test (optional name filter), prints the report.
cd "$(dirname "$0")"
python mcp.py call editor_stop '{}' >/dev/null 2>&1
python mcp.py call editor_play '{}' >/dev/null
sleep 4
python mcp.py call invoke_static "{\"typeName\":\"ImGuiTests.ImGuiSelfTest\",\"method\":\"Start\",\"args\":[\"$1\"]}" >/dev/null
for i in $(seq 1 60); do
  sleep 4
  r=$(python mcp.py call invoke_static '{"typeName":"ImGuiTests.ImGuiSelfTest","method":"Report","args":[]}' | python -c "import sys,json; print(json.load(sys.stdin).get('returned',''))")
  case "$r" in DONE*) break;; esac
done
echo "$r"
