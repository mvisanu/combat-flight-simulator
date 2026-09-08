"""Start the locally installed Blender MCP add-on in a dedicated Blender session."""
import bpy
import sys
from pathlib import Path

addon_dir = Path.home() / 'AppData/Roaming/Blender Foundation/Blender/5.1/scripts/addons'
sys.path.insert(0, str(addon_dir))
import addon
if not hasattr(bpy.types.Scene, 'blendermcp_use_polyhaven'):
    addon.register()
bpy.types.blendermcp_server = addon.BlenderMCPServer(host='127.0.0.1', port=9876)
bpy.types.blendermcp_server.start()
