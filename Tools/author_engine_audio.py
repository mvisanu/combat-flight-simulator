"""Create a seamless authored engine loop from John Veit's CC0 Mustang recording.
Requires numpy and imageio-ffmpeg. Source and exact edit are retained for provenance.
"""
from pathlib import Path
import urllib.request, subprocess, wave
import numpy as np
import imageio_ffmpeg

root = Path(__file__).resolve().parents[1]
source = root / 'ArtSource/Audio/mustangs-paine-field-2010.ogv'
source.parent.mkdir(parents=True, exist_ok=True)
url = 'https://upload.wikimedia.org/wikipedia/commons/9/9c/3_P-51_Mustangs_start-up_and_make_a_fly-by_at_Paine_Field_USA_June_2010_with_sound.ogv'
if not source.exists():
    request = urllib.request.Request(url, headers={'User-Agent':'PacificCombatAssetBuild/1.0'})
    with urllib.request.urlopen(request) as response: source.write_bytes(response.read())
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
raw = subprocess.check_output([ffmpeg,'-v','error','-ss','30','-i',str(source),'-t','8','-vn','-ac','1','-ar','22050','-af','highpass=f=55,lowpass=f=3000','-f','f32le','-'])
samples = np.frombuffer(raw,dtype='<f4').copy()
overlap = 11025
blend = np.linspace(0,1,overlap)
loop = np.concatenate([samples[overlap:-overlap], samples[-overlap:]*(1-blend)+samples[:overlap]*blend])
loop = loop / max(.001,np.max(np.abs(loop))) * .8
out = root / 'Assets/Game/Resources/Audio/MerlinCruise.wav'
out.parent.mkdir(parents=True,exist_ok=True)
with wave.open(str(out),'wb') as file:
    file.setnchannels(1); file.setsampwidth(2); file.setframerate(22050)
    file.writeframes((loop*32767).astype('<i2').tobytes())
print(f'Authored {len(loop)/22050:.1f}s mono loop: {out}')
