/**
 * StoneHammer v10.0: Props Module
 * High-level asset spawning (Voxel Actors, Recipe Landmarks).
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.generateHumanoidParts = function (colors) {
        const skin = this.getProp(colors, "Skin") || "#F1C27D";
        const cloth = this.getProp(colors, "Cloth") || this.getProp(colors, "Shirt") || "#4E342E";
        const accent = this.getProp(colors, "Accent") || this.getProp(colors, "Pants") || "#3E2723";
        const charcoal = "#333333";
        const wood = "#5D4037";

        return [
            { id: "torso", shape: "Box", scale: [0.8, 1.2, 0.4], pos: [0, 1.6, 0], color: cloth },
            { id: "head", shape: "Box", scale: [0.5, 0.5, 0.5], pos: [0, 2.4, 0], color: skin },
            { id: "arm_l", shape: "Box", scale: [0.25, 1.0, 0.25], pos: [-0.6, 1.6, 0], color: skin },
            { id: "arm_r", shape: "Box", scale: [0.25, 1.0, 0.25], pos: [0.6, 1.6, 0], color: skin },
            { id: "leg_l", shape: "Box", scale: [0.3, 1.0, 0.3], pos: [-0.25, 0.5, 0], color: accent },
            { id: "leg_r", shape: "Box", scale: [0.3, 1.0, 0.3], pos: [0.25, 0.5, 0], color: accent },
            { id: "helmet", shape: "Box", scale: [0.6, 0.2, 0.6], pos: [0, 0.3, 0], color: charcoal, parentLimb: "head" },
            { id: "hammer_handle", shape: "Box", scale: [0.1, 0.8, 0.1], pos: [0, -0.6, 0.4], rotation: [90, 0, 0], color: wood, parentLimb: "arm_r" },
            { id: "hammer_head", shape: "Box", scale: [0.4, 0.4, 0.8], pos: [0, 0.4, 0], color: charcoal, parentLimb: "hammer_handle" }
        ];
    };

    sh.spawnVoxel = function (asset, name, isPlayer, transform, metadata) {
        // v15.5: Singleton Enforcement (Prevent Double Spawns)
        // Check if this actor already exists and remove it to prevent "Ghost Nodes"
        const existingNode = this.scene.getTransformNodeByName("voxel_" + name) || this.scene.getMeshByName("voxel_" + name);
        if (existingNode) {
            console.warn(`[Props] Re-spawning '${name}'. Disposing existing instance (ID: ${existingNode.uniqueId}).`);
            existingNode.dispose();
        }

        let group;
        if (isPlayer) {
            // Player requires a Mesh root for .moveWithCollisions()
            // Create a collider box (approx human size)
            group = BABYLON.MeshBuilder.CreateBox("voxel_" + name, { width: 1, height: 2, depth: 1 }, this.scene);
            group.visibility = 0; // Invisible collider
            group.checkCollisions = true;
            group.ellipsoid = new BABYLON.Vector3(0.5, 1.0, 0.5); // Collision ellipsoid
            group.ellipsoidOffset = new BABYLON.Vector3(0, 1.0, 0); // Center of body

            this.log("Player Root created as Mesh (Collider)", "lime");
        } else {
            group = new BABYLON.TransformNode("voxel_" + name, this.scene);
        }

        if (metadata) group.metadata = metadata;

        // Handle rotation if provided
        let rawParts = this.getProp(asset, "Parts");
        let rawColors = this.getProp(asset, "ProceduralColors");

        const parts = (rawParts && rawParts.length > 0) ? rawParts : this.generateHumanoidParts(rawColors);

        if (!parts || parts.length === 0) {
            this.log("WARNING: No parts generated for " + name, "orange");
        } else {
            if (!rawParts) {
                this.log("Generating Humanoid Parts for " + name + " (Skin: " + (this.getProp(rawColors, "Skin") || "Default") + ")", "cyan");
            }
        }

        const meshMap = {};
        parts.forEach(p => {
            // Handle C# vs JS naming (Dimensions -> scale, Offset -> pos)
            const scale = this.getProp(p, "Scale") || this.getProp(p, "Dimensions") || [1, 1, 1];
            const pos = this.getProp(p, "Pos") || this.getProp(p, "Position") || this.getProp(p, "Offset") || [0, 0, 0];
            const id = this.getProp(p, "Id") || p.id; // p.id might be direct from JS generator

            const mesh = BABYLON.MeshBuilder.CreateBox(name + "_" + id, {
                width: scale[0],
                height: scale[1],
                depth: scale[2]
            }, this.scene);

            // Debug: Log naming pattern
            //if (name === "Player") {
            //    console.log(`[Props] Created Player Part: ${mesh.name} (Id: ${id})`);
            //}

            mesh.position = new BABYLON.Vector3(pos[0], pos[1], pos[2]);

            const rot = this.getProp(p, "Rotation");
            if (rot) {
                mesh.rotation = new BABYLON.Vector3(
                    BABYLON.Tools.ToRadians(rot[0]),
                    BABYLON.Tools.ToRadians(rot[1]),
                    BABYLON.Tools.ToRadians(rot[2])
                );
            }
            meshMap[id] = mesh;

            const parentLimb = this.getProp(p, "ParentLimb");
            if (parentLimb && meshMap[parentLimb]) {
                mesh.parent = meshMap[parentLimb];
            } else {
                mesh.parent = group;
            }

            // Debug Parent
            //if (name === "Player") {
            //    console.log(`[Props] ${mesh.name} parent set to: ${mesh.parent ? mesh.parent.name : "NULL"}`);
            //}

            if (id.includes("arm") || id.includes("leg")) {
                mesh.setPivotPoint(new BABYLON.Vector3(0, scale[1] / 2, 0));
            }

            const mat = new BABYLON.StandardMaterial("mat_" + name + "_" + id, this.scene);
            mat.diffuseColor = BABYLON.Color3.FromHexString(this.getProp(p, "Color") || this.getProp(p, "HexColor") || p.color || "#FFFFFF");
            mesh.material = mat;
        });

        const pos = this.parseVec3(this.getProp(transform, "Position"));
        group.position = pos;

        const scale = this.parseVec3(this.getProp(transform, "Scale"), 1);
        group.scaling = scale;

        const rot = this.parseVec3(this.getProp(transform, "Rotation"));
        group.rotation = new BABYLON.Vector3(
            BABYLON.Tools.ToRadians(rot.x),
            BABYLON.Tools.ToRadians(rot.y),
            BABYLON.Tools.ToRadians(rot.z)
        );

        if (isPlayer) {
            this.player = group;
            const camTarget = new BABYLON.TransformNode("camTarget", this.scene);
            camTarget.position = new BABYLON.Vector3(0, 1.5, 0);
            camTarget.parent = this.player;
            this.camera.lockedTarget = camTarget;
            this.log("Player registered and camera locked", "cyan");
        }

        if (transform && transform.isTrigger) {
            this.buildingTriggers.push({ name: name, pos: pos, radius: transform.triggerRadius || 3 });
            this.log("Trigger: " + name, "orange");
        }

        // v15.3: Post-Spawn Hierarchy Check
        if (name === "Player") {
            const playerNodes = this.scene.transformNodes.filter(n => n.name === "voxel_Player");
            console.log(`[Props] Spawn Inspection: Found ${playerNodes.length} 'voxel_Player' nodes.`);

            playerNodes.forEach((node, idx) => {
                console.log(`[Props] Node #${idx} Children:`);
                node.getChildren(null, false).forEach(c => console.log(`   - ${c.name} (Parent: ${c.parent.name})`));
            });

            // Verify if group matches this.player
            if (this.player === group) {
                console.log("[Props] this.player matches current group.");
            } else {
                console.warn("[Props] this.player DOES NOT match current group!");
            }
        }

        this.log("Spawned Actor: " + name, "lime");
    };

    sh.spawnRecipe = function (asset, name, transform, metadata) {
        const group = new BABYLON.TransformNode("recipe_" + name, this.scene);
        if (metadata) group.metadata = metadata;
        const parts = this.getProp(asset, "Parts") || [];

        // v24.0: Optimization - Mesh Merging
        const meshesByMat = {};

        parts.forEach(p => {
            const shape = (this.getProp(p, "Shape") || "Box").toLowerCase();
            let mesh;
            const scale = this.getProp(p, "Scale") || [1, 1, 1];
            const w = scale[0];
            const h = scale[1];
            const d = scale[2];
            const id = this.getProp(p, "Id");

            if (shape === "cylinder") {
                mesh = BABYLON.MeshBuilder.CreateCylinder(name + "_" + id, { height: h, diameter: w }, this.scene);
            } else if (shape === "sphere") {
                mesh = BABYLON.MeshBuilder.CreateSphere(name + "_" + id, { diameter: w }, this.scene);
            } else {
                mesh = BABYLON.MeshBuilder.CreateBox(name + "_" + id, { width: w, height: h, depth: d }, this.scene);
            }
            mesh.checkCollisions = true;

            mesh.position = this.parseVec3(this.getProp(p, "Position"));
            const rot = this.getProp(p, "Rotation") || [0, 0, 0];

            if (rot) mesh.rotation = new BABYLON.Vector3(
                BABYLON.Tools.ToRadians(rot[0]),
                BABYLON.Tools.ToRadians(rot[1]),
                BABYLON.Tools.ToRadians(rot[2])
            );

            // Restore Texture/Material Logic
            const mat = this.createMaterial(name + "_" + id, p);
            mesh.material = mat;

            // v27.5: Animation Safety
            // If this part is targeted by an animation, DO NOT merge it.
            // We need to know if 'id' is in the Timeline.
            // Let's pre-calculate this set outside the loop for performance?
            // Actually, let's just do it inline here for safety if we access asset.Timeline efficiently.

            const timeline = this.getProp(asset, "Timeline");
            let isAnimated = false;
            if (timeline) {
                isAnimated = timeline.some(t => this.getProp(t, "TargetId") === id);
            }

            // Merging Logic: Only merge if NO parent specified AND not animated
            const parentId = this.getProp(p, "ParentId");
            if (!parentId && !isAnimated) {
                if (!meshesByMat[mat.uniqueId]) meshesByMat[mat.uniqueId] = [];
                meshesByMat[mat.uniqueId].push(mesh);
            } else {
                // Keep hierarchy for complex props OR animated parts
                if (parentId) {
                    const parent = this.scene.getNodeByName(name + "_" + parentId);
                    if (parent) mesh.parent = parent;
                    else mesh.parent = group;
                } else {
                    // Root level but animated (kept separate)
                    mesh.parent = group;
                }
            }
        });

        // Perform Merge
        for (const matId in meshesByMat) {
            const list = meshesByMat[matId];
            if (list.length > 0) {
                // Use the first material (they should all be unique instances from cache now, or compatible)
                const mat = list[0].material;
                if (list.length === 1) {
                    list[0].parent = group;
                } else {
                    const merged = BABYLON.Mesh.MergeMeshes(list, true, true, undefined, false, true);
                    if (merged) {
                        merged.name = "merged_" + matId;
                        merged.parent = group;
                        merged.material = mat;
                        merged.checkCollisions = true; // Fix: Enable collisions on merged result
                        //this.log("Merged " + list.length + " meshes for mat " + matId, "gray");
                    }
                }
            }
        }

        // Apply Prefab-level Transform
        const pos = this.parseVec3(this.getProp(transform, "Position"));
        group.position = pos;

        const scale = this.parseVec3(this.getProp(transform, "Scale"), 1);
        group.scaling = scale;

        const rot = this.parseVec3(this.getProp(transform, "Rotation"));
        group.rotation = new BABYLON.Vector3(
            BABYLON.Tools.ToRadians(rot.x),
            BABYLON.Tools.ToRadians(rot.y),
            BABYLON.Tools.ToRadians(rot.z)
        );

        if (transform && transform.isTrigger) {
            this.buildingTriggers.push({ name: name, pos: pos, radius: transform.triggerRadius || 3 });
        }

        // v20.9: New Animation Support
        const timeline = this.getProp(asset, "Timeline");
        if (timeline && timeline.length > 0 && typeof sh.playTimeline === 'function') {
            sh.playTimeline(timeline, group);
        }

        this.log("Spawned Prop: " + name, "lime");
    };

    sh.createBackgroundBuildings = function () {
        var buildingMat = new BABYLON.PBRMaterial("bgBuildingMat", this.scene);
        buildingMat.albedoColor = new BABYLON.Color3(0.2, 0.2, 0.25);
        buildingMat.roughness = 0.9;
        buildingMat.metallic = 0.1;

        for (var i = 0; i < 40; i++) {
            var w = 8 + Math.random() * 15;
            var h = 15 + Math.random() * 50;
            var d = 8 + Math.random() * 15;
            var building = BABYLON.MeshBuilder.CreateBox("bg_building_" + i, { width: w, height: h, depth: d }, this.scene);
            var angle = Math.random() * Math.PI * 2;
            var dist = 60 + Math.random() * 100;
            building.position.x = Math.cos(angle) * dist;
            building.position.z = Math.sin(angle) * dist;
            building.position.y = -5;
            building.material = buildingMat;
        }
    };

    // v19.2: Actor Removal (Combat Death)
    sh.removeActor = function (id) {
        // Try exact name first
        let mesh = this.scene.getMeshByName(id) || this.scene.getTransformNodeByName(id);

        // Try voxel prefix
        if (!mesh) {
            mesh = this.scene.getMeshByName("voxel_" + id) || this.scene.getTransformNodeByName("voxel_" + id);
        }

        if (mesh) {
            this.log(id + " has fallen!", "red");

            // Simple Death Effect: Shrink to nothing
            // (In future, spawn particle system)
            var anim = new BABYLON.Animation("deathScale", "scaling", 30, BABYLON.Animation.ANIMATIONTYPE_VECTOR3, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            var keys = [];
            keys.push({ frame: 0, value: mesh.scaling.clone() });
            keys.push({ frame: 20, value: new BABYLON.Vector3(0.1, 0.1, 0.1) });
            anim.setKeys(keys);

            mesh.animations = [];
            mesh.animations.push(anim);

            this.scene.beginAnimation(mesh, 0, 20, false, 1, () => {
                mesh.dispose();
            });
        } else {
            console.warn("Could not find actor to remove: " + id);
        }
    };

    // v20.8: Dynamic Attachment
    sh.attachAsset = async function (assetUrl, parentId, boneName) {
        console.log(`[Props] Attaching ${assetUrl} to ${parentId}/${boneName}`);

        // 1. Find Parent
        let parentRoot = this.scene.getTransformNodeByName("voxel_" + parentId) || this.scene.getMeshByName("voxel_" + parentId);
        if (!parentRoot) {
            console.warn("Parent not found: " + parentId);
            return;
        }

        // 2. Find Bone (Limb)
        // Fuzzy search for children
        let bone = parentRoot.getChildren((n) => n.name.toLowerCase().includes(boneName.toLowerCase()), false)[0];

        // Fallback: If bone not found, use Hand R
        if (!bone) bone = parentRoot.getChildren((n) => n.name.includes("arm_r") || n.name.includes("right"), false)[0];

        if (!bone) {
            console.warn("Bone/Limb not found on parent. Attaching to root.");
            bone = parentRoot;
        }

        // 3. Load Asset JSON
        try {
            const response = await fetch(assetUrl);
            const assetData = await response.json();

            // 4. Create Asset Mesh (simplified spawnVoxel logic)
            // We give it a unique name
            const childName = parentId + "_attached_" + assetData.id;

            // If exists, remove first
            const existing = this.scene.getMeshByName("voxel_" + childName);
            if (existing) existing.dispose();

            // Spawn using internal spawn logic but treat 'bone' as parent group
            // We manually build the parts because spawnVoxel assumes a world root
            const parts = this.getProp(assetData, "Parts") || [];
            parts.forEach(p => {
                const scale = this.getProp(p, "Scale") || [1, 1, 1];
                const pos = this.getProp(p, "Position") || [0, 0, 0];
                const id = this.getProp(p, "Id") || p.id;
                const color = this.getProp(p, "Color") || "#FFFFFF";
                const rot = this.getProp(p, "Rotation") || [0, 0, 0];

                const mesh = BABYLON.MeshBuilder.CreateBox(childName + "_" + id, {
                    width: scale[0], height: scale[1], depth: scale[2]
                }, this.scene);

                mesh.parent = bone;
                mesh.position = new BABYLON.Vector3(pos[0], pos[1], pos[2]);
                mesh.rotation = new BABYLON.Vector3(
                    BABYLON.Tools.ToRadians(rot[0]),
                    BABYLON.Tools.ToRadians(rot[1]),
                    BABYLON.Tools.ToRadians(rot[2])
                );

                const mat = new BABYLON.StandardMaterial("mat_" + childName, this.scene);
                mat.diffuseColor = BABYLON.Color3.FromHexString(color);
                mesh.material = mat;
            });

            console.log("Attached successfully.");

        } catch (err) {
            console.error("Failed to attach asset: " + err);
        }
    };
})();
