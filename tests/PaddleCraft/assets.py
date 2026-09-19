"""Prevent retired imported content from returning to the runtime build."""
from pathlib import Path
import json,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2]
assert json.loads((root/'assets/ships/content-roots.json').read_text())==[]
for name in ['assets/ships/helmsman-ships','assets/ships/redesign/models.bin.gz']:
 assert not (root/name).exists(),f'Retired payload still in assets: {name}'
project=ET.parse(root/'src/Helmsman/Helmsman.csproj')
resources={node.attrib.get('LogicalName') for node in project.iter('EmbeddedResource')}
assert not resources&{'Helmsman.Ships.bundle','Helmsman.Ships.boatyard'}
assert len(list((root/'assets/ships/final').glob('*.bin.gz')))==9
assert len(list((root/'assets/workshop').glob('*.bin.gz')))==23
catalog=(root/'src/Helmsman.Core/HarborCatalog.cs').read_text()
for retired in ['Enguias','Peixes','RedePesca','OilPress']:
 assert f'new HarborEntry("{retired}"' not in catalog
for retained in ['FishingDock','ShipConstruction','ShipConstruction1','ShipConstruction2','PierCrane1','PierCrane2','PulleyCobia','PulleyElephantSeal','PulleyMarlin']:
 assert f'new HarborEntry("{retained}"' in catalog
for name in ['ImportedHulls.cs','HarborRegistration.cs','BoatyardModels.cs']:
 s=(root/'src/Helmsman'/name).read_text()
 assert 'AssetBundle' not in s and 'Helmsman.Ships.bundle' not in s and 'Helmsman.Ships.boatyard' not in s
print('PASS: no imported roots, bundle, overlay or runtime loader; nine fleet models and all native workshop/harbor resources retained.')
