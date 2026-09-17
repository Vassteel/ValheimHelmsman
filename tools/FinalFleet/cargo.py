"""Retain authored cargo shapes; settle loose props and build a smooth invisible load surface."""
import ast
import math
import numpy as np
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def tree(objects):
    verts, faces = [], []
    for ob in objects:
        if ob.type != 'MESH':
            continue
        offset = len(verts)
        verts.extend(ob.matrix_world @ v.co for v in ob.data.vertices)
        faces.extend([offset + i for i in p.vertices] for p in ob.data.polygons)
    return BVHTree.FromPolygons(verts, faces)


def restore(kind, src, ship, env, root):
    # Replay just the original cargo layout, retaining native textures and hull.
    # Group each prop with its hoops, ties and seams so settling never separates them.
    text = (src / 'build_model.py').read_text()
    if kind == 'ottar':
        start, end = text.index('# Illustrative removable cargo'), text.index('# Low working gear')
    else:
        start = text.index('for x in [1.1,2.39,3.68]:')
        end = text.index('# Compact cargo handling boom')
    for ob in list(ship.objects):
        if ob.name.startswith('Cargo /'):
            # Freighter deck props are rebuilt below as well.
            bpy.data.objects.remove(ob, do_unlink=True)
    materials = bpy.data.materials
    env = dict(env)
    env.update(hide_mats=[m for m in materials if 'hide' in m.name.lower()][:3],
               sackmat=next(m for m in materials if 'sacks' in m.name.lower()),
               bindmat=next(m for m in materials if 'cargo lashings' in m.name.lower() or 'dark cargo hemp' in m.name.lower()),
               bale_mat=next(m for m in materials if 'cargo wrapping' in m.name.lower()))
    exec(compile((root / 'design/big-cargo-v1/cargo_primitives.py').read_text(), 'cargo shapes', 'exec'), env)
    groups = []
    for name in ['barrel', 'hide_bale', 'sack', 'bundle']:
        original = env[name]
        def wrapped(*args, _fn=original, _name=name, **kwargs):
            before = set(ship.objects)
            _fn(*args, **kwargs)
            groups.append((_name, list(set(ship.objects) - before)))
        env[name] = wrapped
    nodes = ast.parse(text[start:end]).body
    nodes = [n for n in nodes if not isinstance(n, ast.FunctionDef) and not
             (isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id in ['hide_mats', 'sackmat', 'bindmat'] for t in n.targets))]
    exec(compile(ast.Module(body=nodes, type_ignores=[]), 'original cargo layout', 'exec'), env)
    if kind == 'freighter':
        env['deck_height'] = lambda x: 2.34 + .09 * (abs(x) - 4.85) + .047
        start = text.index('for x in [-6.4,6.6]:')
        end = text.index('for x in [-8.5,8.5]:')
        exec(compile(text[start:end], 'freighter deck cargo', 'exec'), env)
        # Visible timber skids support the original bins and log pile above the sole.
        for x in [1.1, 2.39, 3.68]:
            for y in [-.63, .63]:
                for dx in [-.36, .36]:
                    env['box']('Freight / bin skid', (x + dx, y, .89), (.12, .92, .12), env['oak'], .006)
        for x in [-3.8, -1.1]:
            env['box']('Freight / timber skid', (x, 0, .85), (.18, 3.25, .06), env['oak'], .005)
    props = {ob for _, objects in groups for ob in objects}
    base = [ob for ob in ship.objects if ob not in props and any(k in ob.name.lower() for k in
            ['floor', 'hold ceiling', 'clinker strake', 'keelson', 'mast step', 'deck plank', 'side working', 'landing', 'end platform', 'freight / round timber', 'freight / ore bin'])]
    report = []
    # Bottom-first settling allows a roll to rest on a bale, and the bale on barrels.
    groups.sort(key=lambda g: min((ob.matrix_world @ v.co).z for ob in g[1] for v in ob.data.vertices))
    for name, objects in groups:
        solid = [ob for ob in objects if not any(k in ob.name.lower() for k in ['binding', 'neck tie', 'seam', 'joint', 'knot', 'hoop', 'edge'])]
        vertices = [ob.matrix_world @ v.co for ob in solid for v in ob.data.vertices]
        low = min(v.z for v in vertices)
        support = tree(base)
        gaps = []
        for v in vertices:
            if v.z > low + .12:
                continue
            hit, _, _, _ = support.ray_cast(Vector((v.x, v.y, low + .035)), Vector((0, 0, -1)), 5)
            if hit is not None:
                gaps.append(v.z - hit.z)
        if not gaps:
            raise AssertionError(('Cargo without support', kind, name, low))
        drop = max(0, min(gaps) - .003)
        for ob in objects:
            ob.location.z -= drop
        bpy.context.view_layer.update()
        base.extend(solid)
        report.append({'shape': name, 'lowered': round(drop, 5), 'support_gap': round(min(gaps) - drop, 5)})
    return report


def walking(kind, ship, edge, walkz, width, unity):
    cargo = [o for o in ship.objects if o.name.startswith(('Cargo /', 'Freight /')) and
             not any(k in o.name.lower() for k in ['lashing', 'stake', 'rope', 'binding', 'neck tie', 'knot'])]
    surface = tree(cargo)
    nx, ny = 12, 6
    xs = np.linspace(-edge, edge, nx + 1)
    # Join the real side walkways rather than covering the entire gunwale.
    half = 1.74 if kind == 'ottar' else 2.24
    ys = np.linspace(-half, half, ny + 1)
    heights = np.full((nx + 1, ny + 1), walkz)
    for i, x in enumerate(xs):
        for j, y in enumerate(ys):
            hits = []
            for dx, dy in [(0, 0), (.14, 0), (-.14, 0), (0, .14), (0, -.14)]:
                hit, _, _, _ = surface.ray_cast(Vector((x + dx, y + dy, 4.5)), Vector((0, 0, -1)), 5)
                if hit is not None:
                    hits.append(hit.z)
            if hits:
                heights[i, j] = max(walkz, max(hits) - .035)
    # Preserve exact deck/boarding heights; grade the interior at <= 30 degrees.
    boundary = walkz - (.038 if kind == 'ottar' else .013)
    heights[0, :] = heights[-1, :] = boundary
    heights[:, 0] = heights[:, -1] = boundary
    for i, x in enumerate(xs):
        if abs(x) < .55:
            heights[i, 0:2] = walkz
            heights[i, -2:] = walkz
    step_x = (xs[1] - xs[0]) * .36
    step_y = (ys[1] - ys[0]) * .36
    for _ in range(nx + ny):
        for i in range(nx + 1):
            for j in range(ny + 1):
                for di, dj, limit in [(1, 0, step_x), (-1, 0, step_x), (0, 1, step_y), (0, -1, step_y)]:
                    if 0 <= i + di <= nx and 0 <= j + dj <= ny:
                        heights[i, j] = min(heights[i, j], heights[i + di, j + dj] + limit)
    solids = []
    # Triangular prisms are truly convex even when the four grid corners aren't coplanar.
    faces = [(0, 2, 1), (3, 4, 5), (0, 1, 4), (0, 4, 3), (1, 2, 5), (1, 5, 4), (2, 0, 3), (2, 3, 5)]
    for i in range(nx):
        for j in range(ny):
            corners = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            for indices in [(0, 1, 2), (0, 2, 3)]:
                triangle = [corners[k] for k in indices]
                vertices = [unity((xs[a], ys[b], heights[a, b] + dz)) for dz in [-.20, 0] for a, b in triangle]
                solids.append({'vertices': [float(c) for v in vertices for c in v],
                               'triangles': [v for f in faces for v in reversed(f)]})
    def height(x, y):
        return float(heights[int(np.clip(round((x + edge) / (2 * edge) * nx), 0, nx)),
                             int(np.clip(round((y + half) / (2 * half) * ny), 0, ny))])
    return solids, {'x': xs.tolist(), 'y': ys.tolist(), 'heights': heights.tolist()}, height
