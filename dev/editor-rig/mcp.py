"""Tiny MCP client for the in-editor sbox-mcp server (streamable HTTP, JSON-RPC).

usage:
  python mcp.py list [query]                   # tool_search
  python mcp.py call <tool> '<json args>'      # tool_call
  python mcp.py raw <method> '<json params>'
"""
import json, sys, urllib.request, os

_url_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".mcp-url")
URL = os.environ.get("IMGUI_MCP_URL") or (open(_url_file).read().strip() if os.path.exists(_url_file) else "http://127.0.0.1:8433/sbox-mcp")
_session = {}

def rpc(method, params=None, _id=[0]):
    _id[0] += 1
    body = json.dumps({"jsonrpc": "2.0", "id": _id[0], "method": method, "params": params or {}}).encode()
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if "sid" in _session:
        headers["Mcp-Session-Id"] = _session["sid"]
    req = urllib.request.Request(URL, data=body, headers=headers, method="POST")
    with urllib.request.urlopen(req, timeout=600) as r:
        sid = r.headers.get("Mcp-Session-Id")
        if sid:
            _session["sid"] = sid
        raw = r.read().decode("utf-8", "replace")
    # SSE or plain JSON
    if raw.lstrip().startswith("event:") or raw.lstrip().startswith("data:"):
        datas = [l[5:].strip() for l in raw.splitlines() if l.startswith("data:")]
        raw = datas[-1] if datas else "{}"
    return json.loads(raw) if raw.strip() else {}

def init():
    rpc("initialize", {"protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "imgui-rig", "version": "1"}})
    try:
        rpc("notifications/initialized")
    except Exception:
        pass

def text_of(result):
    r = result.get("result", result)
    parts = r.get("content", []) if isinstance(r, dict) else []
    out = "\n".join(p.get("text", "") for p in parts if isinstance(p, dict))
    return out if out else json.dumps(result, indent=1)

def main():
    init()
    cmd = sys.argv[1]
    if cmd == "list":
        q = sys.argv[2] if len(sys.argv) > 2 else ""
        print(text_of(rpc("tools/call", {"name": "tool_search", "arguments": {"query": q}})))
    elif cmd == "call":
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        print(text_of(rpc("tools/call", {"name": "tool_call", "arguments": {"name": sys.argv[2], "arguments": args}})))
    elif cmd == "raw":
        params = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        print(json.dumps(rpc(sys.argv[2], params), indent=1))

if __name__ == "__main__":
    main()
