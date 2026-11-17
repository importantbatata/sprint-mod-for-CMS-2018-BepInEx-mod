import json,subprocess,pathlib
repo='importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod'
for r in json.loads(pathlib.Path('release-manifest.json').read_text()):
 v=r['version']; tag='v'+v
 title=f'Sprint Mod {tag}'
 if v=='1.0.0':title+=' — KNOWN BROKEN / historical archive'
 elif r['prerelease']:title+=' — Preview' if v=='1.3.0' else ' — Archived preview'
 else:title+=' — Historical release'
 notes=r['notes']+'\n\n'
 notes+='Download the versioned ZIP under **Assets** to obtain the compiled plugin. Install only one version. '
 notes+=('Merge the included BepInEx folder into your game folder.' if v=='1.3.0' else 'Copy the DLL from the archive into BepInEx/plugins.')
 notes+='\n\n'+('Exact source is included.' if r['source_available'] else 'The exact source snapshot was not retained; this release preserves the original compiled archive.')
 notes+='\n\nThe SHA256SUMS asset verifies the original versioned ZIP. See the main README for requirements and usage.'
 if v=='1.0.0':notes+='\n\n**Do not install this version for normal play. It is retained for historical reference only.**'
 p=pathlib.Path('release-assets')/(tag+'-notes.md');p.write_text(notes+'\n')
 # Stop if a release already exists; never replace existing assets silently.
 if subprocess.run(['gh','release','view',tag,'--repo',repo],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0:
  print(f'{tag} already exists; left unchanged.');continue
 cmd=['gh','release','create',tag,'--repo',repo,'--verify-tag','--title',title,'--notes-file',str(p),'--latest=false','release-assets/'+r['asset'],'release-assets/'+tag+'-SHA256SUMS.txt']
 if r['prerelease']:cmd+=['--prerelease']
 subprocess.run(cmd,check=True)
subprocess.run(['gh','release','list','--repo',repo],check=True)