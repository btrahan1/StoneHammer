/**
 * StoneHammer v10.0: Props Module
 * High-level asset spawning (Voxel Actors, Recipe Landmarks).
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.generateHumanoidParts = function (colors) {
        const skin = this.getProp(colors, "Skin") || "#F1C27D";
        const cloth = this.getProp(colors, "Cloth") || "#4E342E";
        const accent = this.getProp(colors, "Accent") || "#3E2723";
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

    sh.spawnVoxel = function (asset, name, isPlayer, transform) {
        // v15.5: Singleton Enforcement (Prevent Double Spawns)
        // Check if this actor already exists and remove it to prevent "Ghost Nodes"
        const existingNode = this.scene.getTransformNodeByName("voxel_" + name) || this.scene.getMeshByName("voxel_" + name);
        if (existingNode) {
            console.warn(`[Props] Re-spawning '${name}'. Disposing existing instance (ID: ${existingNode.uniqueId}).`);
            existingNode.dispose();
        }

        const group = new BABYLON.TransformNode("voxel_" + name, this.scene);

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
            if (name === "Player") {
                console.log(`[Props] Created Player Part: ${mesh.name} (Id: ${id})`);
            }

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
            if (name === "Player") {
                console.log(`[Props] ${mesh.name} parent set to: ${mesh.parent ? mesh.parent.name : "NULL"}`);
            }

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

    sh.spawnRecipe = function (asset, name, transform) {
        const group = new BABYLON.TransformNode("recipe_" + name, this.scene);
        const parts = this.getProp(asset, "Parts") || [];

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

            mesh.position = this.parseVec3(this.getProp(p, "Position"));
            const rot = this.getProp(p, "Rotation") || [0, 0, 0];

            if (rot) mesh.rotation = new BABYLON.Vector3(
                BABYLON.Tools.ToRadians(rot[0]),
                BABYLON.Tools.ToRadians(rot[1]),
                BABYLON.Tools.ToRadians(rot[2])
            );

            // Restore Texture/Material Logic
            mesh.material = this.createMaterial(name + "_" + id, p);

            const parentId = this.getProp(p, "ParentId");
            if (parentId) {
                const parent = this.scene.getNodeByName(name + "_" + parentId);
                if (parent) mesh.parent = parent;
                else mesh.parent = group;
            } else {
                mesh.parent = group;
            }
        });

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
})();
