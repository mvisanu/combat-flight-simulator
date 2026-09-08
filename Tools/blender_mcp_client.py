"""Send a Python source file through the installed Blender MCP add-on socket."""
import json
import socket
import sys
from pathlib import Path

if len(sys.argv) > 1:
    source = Path(sys.argv[1]).resolve()
    request = {'type': 'execute_code', 'params': {'code': '__file__ = ' + repr(str(source)) + '\n' + source.read_text(encoding='utf-8')}}
else:
    request = {'type': 'get_scene_info', 'params': {}}
with socket.create_connection(('127.0.0.1', 9876), timeout=300) as connection:
    connection.sendall(json.dumps(request).encode('utf-8'))
    data = b''
    while True:
        chunk = connection.recv(65536)
        if not chunk:
            raise RuntimeError('Blender MCP closed before returning a result')
        data += chunk
        try:
            result = json.loads(data)
            break
        except json.JSONDecodeError:
            continue
print(json.dumps(result, indent=2))
if result.get('status') != 'success':
    sys.exit(1)
