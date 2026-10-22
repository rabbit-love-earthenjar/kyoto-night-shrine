"""Read-only scene checks. Does not substitute for Unity import or human playtests."""
import hashlib
import json
import math
import re
from build_designlab_v1 import ROOT, Scene, ENCOUNTER, GHOST


def bounds(scene, go):
    box = scene.component(go, 61)
    pos, scale = scene.world(scene.transform(go))
    size, offset = scene.vec(box, 'm_Size'), scene.vec(box, 'm_Offset')
    cx, cy = [pos[i] + offset[i]*scale[i] for i in range(2)]
    w, h = [abs(size[i]*scale[i]) for i in range(2)]
    return cx-w/2, cx+w/2, cy-h/2, cy+h/2


def ground_at(scene, x, top):
    for go in scene.blocks:
        if scene.types[go] != 1 or not scene.active(go): continue
        name = scene.field(go, 'm_Name')
        if name == 'Player' or 'Seal' in name or 'Spike' in name: continue
        boxes = [c for c in scene.components(go) if scene.types[c] == 61
                 and scene.field(c, 'm_IsTrigger') == '0']
        if len(boxes) != 1: continue
        left, right, _, surface = bounds(scene, go)
        if left+.5 <= x <= right-.5 and abs(surface-top) < .01: return name
    raise AssertionError(('unsupported checkpoint/actor', x, top))


def jump_checks(scene, names, direction):
    result = []
    for a, b in zip(names, names[1:]):
        ba, bb = [bounds(scene, scene.find(n)) for n in (a,b)]
        # Leave half a unit of margin on each platform; no coyote time assumed.
        launch = ba[1]-.5 if direction > 0 else ba[0]+.5
        landing = bb[0]+.5 if direction > 0 else bb[1]-.5
        distance, rise = abs(landing-launch), bb[3]-ba[3]
        discriminant = 12**2 - 2*29.43*rise
        assert discriminant > 0, (a,b,'rise')
        flight = (12+math.sqrt(discriminant))/29.43
        reach = 6*flight
        assert distance < reach*.9, (a,b,distance,reach)
        result.append({'from':a,'to':b,'marginDistance':round(distance,2),
                       'rise':round(rise,2),'idealReach':round(reach,2)})
    return result


def main():
    config=json.loads((ROOT/'Docs/LevelDesign_DesignLab.templates.json').read_text(encoding='utf-8-sig'))
    result={'scope':'Serialized scene checks and ideal jump estimates, NOT Unity runtime acceptance','scenes':[]}
    for entry in config['templates']:
        source=ROOT/entry['sourceScene']
        assert hashlib.sha256(source.read_bytes()).hexdigest().upper()==entry['initialSha256'].upper()
        copy=ROOT/entry['experimentScene']
        guid=lambda p: re.search(r'^guid: (\w+)',p.read_text(),re.M)[1]
        assert guid(source.with_suffix('.unity.meta'))!=guid(copy.with_suffix('.unity.meta'))
        scene=Scene(entry['experimentScene'])
        scene.verify()
        encounter_count=0
        for component,block in scene.blocks.items():
            if 'guid: '+ENCOUNTER not in block: continue
            encounter_count+=1
            go=scene.ref(component,'m_GameObject')
            assert scene.active(go)
            retry=scene.ref(component,'retryPoint')
            assert scene.field(scene.ref(retry,'m_GameObject'),'m_Name')=='StartPoint'
            player=scene.ref(component,'player')
            assert scene.field(player,'moveSpeed')=='6'
            checkpoint=scene.vec(component,'checkpointPosition')
            ground_at(scene,checkpoint[0],checkpoint[1]-1.3)
            gate=scene.ref(component,'exitBarrier')
            gb=bounds(scene,gate)
            assert gb[3]-gb[2]>4.9 and scene.active(gate)
            for field,expected in [('firstWave',2),('secondWave',3)]:
                section=re.search(r'  '+field+r':\n((?:  - \{fileID: \d+\}\n)+)',block)[1]
                actors=[int(i) for i in re.findall(r'fileID: (\d+)',section)]
                assert len(actors)==expected and len(set(actors))==expected
                for actor in actors:
                    assert scene.field(actor,'m_IsActive')=='0'
                    health=next(c for c in scene.components(actor) if scene.field(c,'maxHP') is not None)
                    assert scene.field(health,'faithPointReward')=='0'
                    assert scene.field(health,'dropStarSealOnDeath')=='0'
                    enemy=scene.component(actor,guid=GHOST)
                    pos,_=scene.world(scene.transform(actor))
                    assert float(scene.field(enemy,'routeMinX'))<=pos[0]<=float(scene.field(enemy,'routeMaxX'))
                    if scene.field(enemy,'movementMode')=='1':
                        ground_at(scene,pos[0],float(scene.field(enemy,'routeSurfaceY')))
        assert encounter_count==2
        inventory=next(c for c,b in scene.blocks.items() if 'guid: b4e5f60123454d6e8f90123456789abc' in b)
        assert scene.field(inventory,'persistResourceState')=='0'
        if 'Stage_1_2_' in entry['experimentScene']:
            for name in ['EndGate','EndGateVisual','EndGate_ClearMarker']:
                assert scene.world(scene.transform(scene.find(name)))[0][0]==208
            jumps=jump_checks(scene,['Lab_Entry','Lab_StepA','Lab_StepB'],1)
        else:
            clouds=[scene.find('Lab_Cloud_%02d'%i) for i in range(1,5)]
            assert all(scene.active(go) for go in clouds)
            assert all(not scene.active(scene.find('TemporaryCloud_%02d'%i)) for i in range(1,12))
            for cloud in clouds:
                assert scene.vec(scene.transform(cloud),'m_LocalScale')==[1,1,1]
            ground_at(scene,30,-4)
            jumps=jump_checks(scene,['Lower_CloudLanding','Lab_Cloud_01','Lab_Cloud_02',
                                    'Lab_CloudRest','Lab_Cloud_03','Lab_Cloud_04','Lower_LeftGoalSolid'],-1)
        result['scenes'].append({'scene':entry['experimentScene'],'sourceHash':'unchanged',
                                'references':'pass','encounterConfiguration':'pass','jumpEstimates':jumps})
    print(json.dumps(result,ensure_ascii=False,indent=2))


if __name__=='__main__': main()
