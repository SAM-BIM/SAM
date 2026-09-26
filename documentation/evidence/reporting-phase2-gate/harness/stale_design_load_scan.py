import zipfile,json,os,sys
G='e5c2659a-3581-4bdf-a3c8-959fe6d4e50e'
def load(p):
    z=zipfile.ZipFile(p); return json.loads(z.read(z.namelist()[0]).decode('utf-8-sig'))
def read(sets,name):
    for s in sets:
        if s.get('Guid')==G:
            for p in s.get('Parameters') or []:
                if p['Name']==name: return p['Value']
            break
    for s in sets:
        if s['Name']=='SAM.Analytical':
            for p in s.get('Parameters') or []:
                if p['Name']==name: return p['Value']
            break
    for s in sets:
        for p in s.get('Parameters') or []:
            if p['Name']==name: return p['Value']
tot=bad=files=0
for root,_,fs in os.walk(sys.argv[1]):
    if 'website-evidence' in root: continue
    for f in fs:
        if not f.endswith('.sam'): continue
        p=os.path.join(root,f)
        try: d=load(p)
        except: continue
        objs=[v['Value'] for g in d.get('AdjacencyCluster',{}).get('Objects',[]) for v in g['Value'] if isinstance(v.get('Value'),dict)]
        res={}
        for o in objs:
            if o.get('_type','').startswith('SAM.Analytical.SpaceSimulationResult'):
                q={pp['Name']:pp.get('Value') for s in o['ParameterSets'] for pp in s.get('Parameters') or []}
                res[(o['Name'],q.get('Load Type'))]=q.get('Design Load')
        fb=0
        for o in objs:
            if o.get('_type','').startswith('SAM.Analytical.Space,'):
                sets=o['ParameterSets']; n=sum(1 for s in sets for pp in s.get('Parameters') or [] if pp['Name']=='Design Heating Load')
                v=read(sets,'Design Heating Load'); r=res.get((o['Name'],'Heating'))
                if r is None: continue
                tot+=1
                if n>1 and v!=r: bad+=1; fb+=1
        if fb: files+=1; print(fb, p)
print('spaces with results:',tot,' stale-read:',bad,' files:',files)
