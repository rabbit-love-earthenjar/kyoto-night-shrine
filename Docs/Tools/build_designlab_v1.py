"""Offline, guarded Unity scene-instance edit. No shared prefab/scene source writes.

Only supports these text-serialized scenes, with axis-aligned template transforms.
Run once on the verified V0 copies. Writes a geometry/reference audit alongside them.
"""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
ENCOUNTER = '2b49f03cf8284e9b96b7b0c71c0a6612'
GHOST = 'c3f6d4a9e28548e0bb2dc34f91f42533'
CAMERA = '3e8b0ec7e2fb4df19cf0d2d8cc8d9a21'
RANGED = '45c5a373e1608474d90342cd3fe4bd8f'
INVENTORY = 'b4e5f60123454d6e8f90123456789abc'


def vector(x, y, z=None):
    return '{x: %g, y: %g%s}' % (x, y, '' if z is None else ', z: %g' % z)


class Scene:
    def __init__(self, path):
        self.path = ROOT / path
        text = self.path.read_text(encoding='utf-8-sig')
        matches = list(re.finditer(r'^--- !u!(\d+) &(-?\d+).*\n', text, re.M))
        self.prefix = text[:matches[0].start()]
        self.blocks = {}
        self.types = {}
        for i, match in enumerate(matches):
            ident = int(match[2])
            assert ident not in self.blocks
            self.blocks[ident] = text[match.start():matches[i+1].start() if i+1 < len(matches) else len(text)]
            self.types[ident] = int(match[1])
        self.counter = 9100000000
        self.audit = {'scene': path, 'platforms': [], 'encounters': [], 'retunedEnemies': []}

    def alloc(self):
        while self.counter in self.blocks:
            self.counter += 1
        answer = self.counter
        self.counter += 1
        return answer

    def field(self, ident, key, default=None):
        match = re.search(r'^  ' + re.escape(key) + r':(?: (.*))?$', self.blocks[ident], re.M)
        return match[1] if match else default

    def put(self, ident, key, value):
        pattern = r'^  ' + re.escape(key) + r':[^\n]*$'
        line = '  ' + key + ': ' + str(value)
        if re.search(pattern, self.blocks[ident], re.M):
            self.blocks[ident] = re.sub(pattern, lambda _: line, self.blocks[ident], flags=re.M)
        else:
            self.blocks[ident] += line + '\n'

    def find(self, name):
        found = [i for i in self.blocks if self.types[i] == 1 and self.field(i, 'm_Name') == name]
        assert len(found) == 1, (name, found)
        return found[0]

    def components(self, go):
        return [int(i) for i in re.findall(r'  - component: \{fileID: (\d+)\}', self.blocks[go])]

    def component(self, go, kind=None, guid=None):
        found = [i for i in self.components(go) if (kind is None or self.types[i] == kind)
                 and (guid is None or 'guid: ' + guid in self.blocks[i])]
        assert len(found) == 1, (self.field(go, 'm_Name'), kind, guid, found)
        return found[0]

    def transform(self, go):
        return self.component(go, 4)

    def ref(self, ident, key):
        return int(re.search(r'fileID: (-?\d+)', self.field(ident, key))[1])

    def vec(self, ident, key):
        return [float(v) for v in re.findall(r'[xyz]: ([\d.eE+\-]+)', self.field(ident, key))]

    def world(self, transform):
        pos, scale = self.vec(transform, 'm_LocalPosition'), self.vec(transform, 'm_LocalScale')
        parent = self.ref(transform, 'm_Father')
        if parent:
            pp, ps = self.world(parent)
            pos = [pp[i]+pos[i]*ps[i] for i in range(3)]
            scale = [scale[i]*ps[i] for i in range(3)]
        return pos, scale

    def active(self, go):
        if self.field(go, 'm_IsActive') != '1':
            return False
        parent = self.ref(self.transform(go), 'm_Father')
        return not parent or self.active(self.ref(parent, 'm_GameObject'))

    def append_child(self, parent, trans):
        if parent:
            key, ident = 'm_Children', parent
        else:
            key = 'm_Roots'
            ident = next(i for i in self.blocks if self.types[i] == 1660057539)
        pattern = r'(  '+key+r':)(?: \[\])?\n'
        self.blocks[ident], count = re.subn(pattern, r'\1\n  - {fileID: '+str(trans)+'}\n', self.blocks[ident], count=1)
        assert count == 1

    def group(self, name):
        go, t = self.alloc(), self.alloc()
        self.types[go], self.types[t] = 1, 4
        self.blocks[go] = f'''--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {t}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
'''
        self.blocks[t] = f'''--- !u!4 &{t}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
'''
        self.append_child(0, t)
        return go

    def mono(self, go, guid, fields):
        ident = self.alloc()
        self.types[ident] = 114
        self.blocks[ident] = f'''--- !u!114 &{ident}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
'''+fields
        self.blocks[go] = self.blocks[go].replace('  m_Component:\n', f'  m_Component:\n  - component: {{fileID: {ident}}}\n', 1)
        return ident

    def clone(self, original, name, parent=0):
        ids = []
        def visit(go):
            ids.append(go)
            ids.extend(self.components(go))
            t = self.transform(go)
            children = re.search(r'  m_Children:[^\n]*\n((?:  - \{fileID: \d+\}\n)*)', self.blocks[t])[1]
            for child in re.findall(r'fileID: (\d+)', children):
                visit(self.ref(int(child), 'm_GameObject'))
        visit(original)
        mapping = {old: self.alloc() for old in ids}
        for old, new in mapping.items():
            text = self.blocks[old]
            text = re.sub(r'^(--- !u!\d+ &)-?\d+', lambda m: m[1]+str(new), text)
            text = re.sub(r'\{fileID: (-?\d+)\}', lambda m: '{fileID: '+str(mapping.get(int(m[1]), int(m[1])))+'}', text)
            self.blocks[new], self.types[new] = text, self.types[old]
        go = mapping[original]
        t = self.transform(go)
        self.put(go, 'm_Name', name)
        self.put(go, 'm_IsActive', 1)
        self.put(t, 'm_Father', '{fileID: %d}' % parent)
        self.append_child(parent, t)
        return go

    def place(self, go, x, y, sx=None, sy=None):
        t = self.transform(go)
        self.put(t, 'm_LocalPosition', vector(x, y, 0))
        if sx is not None:
            self.put(t, 'm_LocalScale', vector(sx, sy, 1))

    def platform(self, name, left, right, top, height=.6):
        go = self.clone(self.find(self.platform_template), name, self.root)
        self.place(go, (left+right)/2, top-height/2, right-left, height)
        box = self.component(go, 61)
        self.put(box, 'm_Size', vector(1, 1))
        self.put(box, 'm_Offset', vector(0, 0))
        self.put(box, 'm_IsTrigger', 0)
        self.audit['platforms'].append({'name': name, 'left': left, 'right': right, 'top': top})
        return go

    def enemy(self, source, name, x, y, bounds, flying=False):
        go = self.clone(self.find(source), name, self.root)
        self.place(go, x, y)
        comp = self.component(go, guid=GHOST)
        settings = dict(useStateMachine=1, movementMode=0 if flying else 1,
                        useRouteMovementBounds=1, routeMinX=bounds[0], routeMaxX=bounds[1],
                        routeSurfaceY=bounds[2], detectRange=6, chaseRange=6,
                        chaseSpeed=2.7 if flying else 3.2, chaseMemoryDuration=1.2,
                        chaseLeashBuffer=0, attackPauseDuration=.26, attackCooldown=.95,
                        attackLungeSpeed=3.5 if flying else 3.2, attackLungeDuration=.12,
                        attackRange=.95, patrolSpeed=.9)
        for k, v in settings.items(): self.put(comp, k, v)
        health = next(i for i in self.components(go) if self.field(i, 'maxHP') is not None)
        self.put(health, 'faithPointReward', 0)
        self.put(health, 'dropStarSealOnDeath', 0)
        self.put(go, 'm_IsActive', 0)
        return go

    def encounter(self, name, left, right, floor, start, entry, ground_source, fly_source):
        go = self.group(name)
        # Gate is above the player's maximum jump rise. Retreat remains possible.
        gatex = right-1 if entry == 'left' else left+1
        gate = self.platform(name+'_Seal', gatex-.22, gatex+.22, floor+5, 5)
        sr = self.component(gate, 212)
        self.put(sr, 'm_Color', '{r: 0.65, g: 0.55, b: 1, a: 0.75}')
        self.put(sr, 'm_SortingOrder', 6)
        low, high = left+3, right-3
        a = self.enemy(ground_source, name+'_PaperA', low+2, floor+.75, (low, high, floor))
        b = self.enemy(ground_source, name+'_PaperB', high-1, floor+.75, (low, high, floor))
        c = self.enemy(ground_source, name+'_PaperC', low+3, floor+.75, (low, high, floor))
        d = self.enemy(ground_source, name+'_PaperD', high-1, floor+.75, (low, high, floor))
        e = self.enemy(fly_source, name+'_Air', (low+high)/2, floor+2.5, (low, high, floor), True)
        lo, hi = (left+2, left+5) if entry == 'left' else (right-5, right-2)
        player = self.transform(self.find('Player'))
        player_comp = next(i for i in self.components(self.find('Player')) if self.field(i,'moveSpeed') is not None)
        start_t = self.transform(self.find('StartPoint'))
        self.mono(go, ENCOUNTER, f'''  player: {{fileID: {player_comp}}}
  retryPoint: {{fileID: {start_t}}}
  checkpointPosition: {vector(start, floor+1.3, 0)}
  triggerMinimum: {vector(lo, floor-.5)}
  triggerMaximum: {vector(hi, floor+3.5)}
  firstWave:
  - {{fileID: {a}}}
  - {{fileID: {b}}}
  secondWave:
  - {{fileID: {c}}}
  - {{fileID: {d}}}
  - {{fileID: {e}}}
  exitBarrier: {{fileID: {gate}}}
  waveBreak: 1.2
  completionFaith: 5
''')
        self.audit['encounters'].append({'name': name, 'bounds': [left,right,floor], 'checkpointX':start,
                                        'waves':[2,3], 'enemyTotal':5, 'maximumWaveActors':3})

    def local_inventory(self):
        go = self.group('DesignLab_LocalInventory')
        inventory = self.mono(go, INVENTORY, '''  faithPoints: 0
  persistAcrossScenes: 0
  persistResourceState: 0
  cafeStarterIngredientsInitialized: 0
  farmStarterSeedsInitialized: 0
  materials: []
''')
        gm = next(i for i in self.components(self.find('GameManager')) if self.field(i,'resourceInventory') is not None)
        self.put(gm, 'resourceInventory', '{fileID: %d}' % inventory)
        self.put(gm, 'clearStoryStageId', '')
        self.put(gm, 'unlockCafeOnClear', 0)

    def verify(self):
        # Unity local references omit guid; external asset references are intentionally excluded.
        for ident, block in self.blocks.items():
            for ref in re.findall(r'\{fileID: (-?\d+)\}', block):
                assert int(ref) == 0 or int(ref) in self.blocks, (ident, 'missing local ref', ref)
        for go in [i for i in self.blocks if self.types[i] == 1]:
            for comp in self.components(go):
                assert self.ref(comp, 'm_GameObject') == go
        for ident in [i for i in self.blocks if self.types[i] == 4]:
            children = re.search(r'  m_Children:[^\n]*\n((?:  - \{fileID: \d+\}\n)*)', self.blocks[ident])
            assert children
            refs = re.findall(r'fileID: (\d+)', children[1])
            assert len(refs) == len(set(refs))
            for child in refs:
                assert self.ref(int(child), 'm_Father') == ident
        self.audit['localReferencesAndParentLinks'] = 'pass'

    def save(self):
        self.verify()
        self.path.write_text(self.prefix+''.join(self.blocks.values()), encoding='utf-8', newline='\n')
        self.audit['sha256'] = hashlib.sha256(self.path.read_bytes()).hexdigest().upper()


def opening(scene):
    scene.root = scene.transform(scene.group('DesignLab_V1_Extension'))
    scene.platform_template = 'EndForeshadowArea_FinalStonePath'
    for name, l, r, top in [('Entry',135,156,.3), ('StepA',158,167,1.2),
                             ('StepB',169,180,.3), ('Courtyard',180,202,.3), ('GateRoad',202,213,.3)]:
        scene.platform('Lab_'+name, l,r,top)
    scene.encounter('Lab_OfferingApproach',135,156,.3,136,'left','PaperDoll_01_Tutorial','GhostLantern_01_Tutorial')
    scene.encounter('Lab_CourtyardPurification',180,202,.3,181,'left','PaperDoll_01_Tutorial','GhostLantern_01_Tutorial')
    for name in ['EndGate','EndGate_ClearMarker','EndGateVisual']:
        go=scene.find(name)
        # Avoid moving a child twice when its gate parent has already moved.
        t=scene.transform(go)
        if name != 'EndGate' and scene.ref(t,'m_Father') == scene.transform(scene.find('EndGate')):
            continue
        p=scene.vec(t,'m_LocalPosition')
        scene.put(t,'m_LocalPosition',vector(p[0]+74,p[1],p[2]))
    camera=next(i for i in scene.blocks if 'guid: '+CAMERA in scene.blocks[i])
    scene.put(camera,'maxX',205)
    fall=scene.find('FallZone')
    t=scene.transform(fall)
    scene.place(fall,106,-6,230,2)
    scene.put(scene.component(fall,61),'m_Size',vector(1,1))
    # Give the two new sections one recovery pickup each; no new material economy.
    for x in [157,201]:
        h=scene.clone(scene.find('HeartPickup_Reward01'),'Lab_Recovery_'+str(x),scene.root)
        scene.place(h,x if x!=157 else 154,.95)
    for i,x in enumerate([159,164,171,176,206]):
        f=scene.clone(scene.find('FaithPointPickup_Reward01'),'Lab_Guide_'+str(i),scene.root)
        scene.place(f,x,2.2 if x<168 else 1.2)
    # Existing placed enemies retain HP/damage; only their readable pursuit improves.
    for go in list(scene.blocks):
        if scene.types[go]!=1 or not scene.active(go): continue
        for c in scene.components(go):
            if 'guid: '+GHOST not in scene.blocks[c]: continue
            for k,v in dict(chaseSpeed=2.6,detectRange=4.5,chaseRange=4.5,
                            chaseMemoryDuration=1,attackPauseDuration=.24,attackLungeSpeed=3,
                            attackLungeDuration=.1).items(): scene.put(c,k,v)
            scene.audit['retunedEnemies'].append(scene.field(go,'m_Name'))
    scene.audit.update(originalGoalX=134, goalX=208, addedMainRouteUnits=74, addedEncounterEnemies=10)


def route(scene):
    scene.root=scene.transform(scene.group('DesignLab_V1_RoutePressure'))
    scene.platform_template='StartArea_StonePath_12u'
    # Replace the eleven equivalent steps with two groups and a stable observation spot.
    cloud_template=scene.find('TemporaryCloud_01')
    for index in range(1,12): scene.put(scene.find('TemporaryCloud_%02d'%index),'m_IsActive',0)
    for index,(x,y) in enumerate([(25,-2.85),(22,-2.25),(14.6,-2.15),(11.5,-2.85)]):
        cloud=scene.clone(cloud_template,'Lab_Cloud_%02d'%(index+1),scene.root)
        scene.place(cloud,x,y,1,1)
        scene.put(scene.component(cloud,61),'m_Size',vector(1.5,.3))
        child = int(re.search(r'm_Children:\n  - \{fileID: (\d+)\}', scene.blocks[scene.transform(cloud)])[1])
        scale = scene.vec(child, 'm_LocalScale')
        scene.put(child, 'm_LocalScale', vector(scale[0]*1.5/1.15, scale[1], scale[2]))
        temp=scene.component(cloud,guid='a15c97e8b4d147779abb0123cd45ef60')
        scene.put(temp,'warningDelay',.8)
        scene.put(temp,'recoveryDelay',2.8)
    scene.platform('Lab_CloudRest',17,20,-2.7,1)
    cloud_checkpoint(scene)
    # Existing route's generous start becomes a short two-wave ground/air encounter.
    scene.encounter('Lab_UpperOffering',8,33,8,9,'left','PaperDoll_Upper_01','Ghost_Upper_03')
    scene.encounter('Lab_LowerLanding',103,129,-4,127,'right','PaperDoll_Lower_01','Ghost_Lower_03')
    for name in ['PaperDoll_Upper_01','PaperDoll_Upper_05','WispRunner_Upper_01',
                 'PaperDoll_Lower_01','Ghost_Lower_03']:
        scene.put(scene.find(name),'m_IsActive',0)
    # Remove the approach crates that overlap the encounter actors' movement lane.
    for name in ['UpperCrate_01','UpperCrate_02','UpperCrate_03']:
        scene.put(scene.find(name),'m_IsActive',0)
    # A single ranged threat guards the wide stone approach, not the tiny cloud landing.
    ranged=scene.find('WispRunner_Lower_02')
    scene.place(ranged,34,scene.vec(scene.transform(ranged),'m_LocalPosition')[1])
    rc=scene.component(ranged,guid=RANGED)
    for k,v in dict(minimumX=30,maximumX=37,attackCooldown=2.0,windupDuration=.55,detectRange=7).items():
        scene.put(rc,k,v)
    # A new healing pickup is earned in each encounter's exit lane.
    for index,(x,y) in enumerate([(32,8.8),(104,-3.2)]):
        h=scene.clone(scene.find('HeartPickup_Reward01'),'Lab_RouteRecovery_'+str(index),scene.root)
        scene.place(h,x,y)
    setup=next(i for i in scene.blocks if 'guid: e590db2cf8154bbb9af04567ab892da4' in scene.blocks[i])
    scene.put(setup,'m_Enabled',0)
    scene.audit.update(originalClouds=11,activeClouds=4,stableRestPlatforms=1,
                       addedEncounterEnemies=10,replacedPatrolEnemies=5,
                       cloudApproachCheckpoint=[30,-2.7])


def cloud_checkpoint(scene):
    go = scene.group('Lab_CloudApproach_Checkpoint')
    player = next(i for i in scene.components(scene.find('Player')) if scene.field(i,'moveSpeed') is not None)
    retry = scene.transform(scene.find('StartPoint'))
    scene.mono(go, '4d13250c57d74d44893b8d7475aa01bc', f'''  player: {{fileID: {player}}}
  retryPoint: {{fileID: {retry}}}
  triggerMinimum: {{x: 29, y: -4.5}}
  triggerMaximum: {{x: 32, y: -0.5}}
  checkpointPosition: {{x: 30, y: -2.7, z: 0}}
''')


def main():
    config=json.loads((ROOT/'Docs/LevelDesign_DesignLab.templates.json').read_text(encoding='utf-8-sig'))
    scenes=[]
    for template in config['templates']:
        source=ROOT/template['sourceScene']
        dest=ROOT/template['experimentScene']
        baseline=template['initialSha256'].lower()
        assert hashlib.sha256(source.read_bytes()).hexdigest()==baseline, 'Source changed; audit first'
        assert hashlib.sha256(dest.read_bytes()).hexdigest()==baseline, 'Copy already edited; do not overwrite'
        scenes.append(Scene(template['experimentScene']))
    opening(scenes[0])
    route(scenes[1])
    for scene in scenes:
        scene.local_inventory()
        scene.verify()
    # All mutations are validated in memory before either scene is written.
    for scene in scenes: scene.save()
    report={'version':'V1','date':'2026-09-14','unityRuntime':'pending',
            'sourceScenesUnchanged':True,'scenes':[s.audit for s in scenes]}
    (ROOT/'Docs/DesignLab_V1.audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False,indent=2))


if __name__=='__main__': main()
