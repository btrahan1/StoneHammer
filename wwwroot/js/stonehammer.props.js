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
        const group = new BABYLON.TransformNode("voxel_" + name, this.scene);
        const parts = (asset.parts && asset.parts.length > 0) ? asset.parts : this.generateHumanoidParts(asset.proceduralColors);

        if (!parts || parts.length === 0) {
            this.log("WARNING: No parts generated for " + name, "orange");
        }

        const meshMap = {};
        parts.forEach(p => {
            const mesh = BABYLON.MeshBuilder.CreateBox(name + "_" + p.id, {
                width: p.scale[0],
                height: p.scale[1],
                depth: p.scale[2]
            }, this.scene);
            mesh.position = new BABYLON.Vector3(p.pos[0], p.pos[1], p.pos[2]);
            if (p.rotation) {
                mesh.rotation = new BABYLON.Vector3(
                    BABYLON.Tools.ToRadians(p.rotation[0]),
                    BABYLON.Tools.ToRadians(p.rotation[1]),
                    BABYLON.Tools.ToRadians(p.rotation[2])
                );
            }
            meshMap[p.id] = mesh;
            if (p.parentLimb && meshMap[p.parentLimb]) {
                mesh.parent = meshMap[p.parentLimb];
            } else {
                mesh.parent = group;
            }
            if (p.id.includes("arm") || p.id.includes("leg")) {
                mesh.setPivotPoint(new BABYLON.Vector3(0, p.scale[1] / 2, 0));
            }
            const mat = new BABYLON.StandardMaterial("mat_" + name + "_" + p.id, this.scene);
            mat.diffuseColor = BABYLON.Color3.FromHexString(p.color || "#FFFFFF");
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
        this.log("Spawned Actor: " + name, "lime");
    };

    sh.spawnRecipe = function (asset, name, transform) {
        const group = new BABYLON.TransformNode("recipe_" + name, this.scene);
        asset.parts.forEach(p => {
            const shape = (p.shape || "Box").toLowerCase();
            let mesh;
            const w = (p.scale && p.scale[0]) || 1;
            const h = (p.scale && p.scale[1]) || 1;
            const d = (p.scale && p.scale[2]) || 1;

            if (shape === "cylinder") {
                mesh = BABYLON.MeshBuilder.CreateCylinder(name + "_" + p.id, { height: h, diameter: w }, this.scene);
            } else if (shape === "sphere") {
                mesh = BABYLON.MeshBuilder.CreateSphere(name + "_" + p.id, { diameter: w }, this.scene);
            } else {
                mesh = BABYLON.MeshBuilder.CreateBox(name + "_" + p.id, { width: w, height: h, depth: d }, this.scene);
            }

            mesh.position = this.parseVec3(p.position);
            if (p.rotation) mesh.rotation = new BABYLON.Vector3(
                BABYLON.Tools.ToRadians(p.rotation[0] || 0),
                BABYLON.Tools.ToRadians(p.rotation[1] || 0),
                BABYLON.Tools.ToRadians(p.rotation[2] || 0)
            );
            mesh.material = this.createMaterial(name + "_" + p.id, p);

            if (p.parentId) {
                const parent = this.scene.getNodeByName(name + "_" + p.parentId);
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
