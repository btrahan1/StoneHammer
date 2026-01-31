window.stoneHammer = {
    engine: null,
    scene: null,
    canvas: null,
    player: null,
    inputMap: {},
    playerSpeed: 0.5,
    playerRotationSpeed: 0.05,

    getProp: function (obj, key) {
        if (!obj) return null;
        return obj[key] ?? obj[key.charAt(0).toLowerCase() + key.slice(1)] ?? obj[key.toUpperCase()] ?? null;
    },

    log: function (msg, color = "white") {
        const el = document.getElementById("debug-log");
        if (el) {
            const div = document.createElement("div");
            div.style.color = color;
            div.innerText = "[" + new Date().toLocaleTimeString() + "] " + msg;
            el.prepend(div);
        }
        console.log("StoneHammer: " + msg);
    },

    init: function (canvasId) {
        try {
            this.log("Engine Init v2.7 (Classic Rotation)", "cyan");
            this.canvas = document.getElementById(canvasId);
            this.engine = new BABYLON.Engine(this.canvas, true);
            this.scene = new BABYLON.Scene(this.engine);

            this.scene.clearColor = new BABYLON.Color4(0.08, 0.08, 0.12, 1);

            // High-Fidelity Camera & Lighting
            this.camera = new BABYLON.ArcRotateCamera("camera1", -Math.PI / 2, Math.PI / 3, 15, BABYLON.Vector3.Zero(), this.scene);
            this.camera.attachControl(this.canvas, true);
            this.camera.upperRadiusLimit = 1000;
            this.camera.lowerRadiusLimit = 4;
            this.camera.wheelPrecision = 50;
            // Removed targetScreenOffset - using camTarget node instead

            var hemi = new BABYLON.HemisphericLight("hemi", new BABYLON.Vector3(0, 1, 0), this.scene);
            hemi.intensity = 0.8;
            hemi.groundColor = new BABYLON.Color3(0.1, 0.1, 0.2);

            var dir = new BABYLON.DirectionalLight("dir", new BABYLON.Vector3(-1, -2, -1), this.scene);
            dir.position = new BABYLON.Vector3(50, 100, 50);
            dir.intensity = 1.6;

            // High-Fidelity Stone Plaza Floor (v1.9)
            var ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 1000, height: 1000 }, this.scene);

            // Check for GridMaterial (Library) or BrickProcedural (Built-in)
            const Grid = BABYLON.GridMaterial || (BABYLON.Materials && BABYLON.Materials.GridMaterial);

            if (Grid) {
                var stoneGrid = new Grid("groundGrid", this.scene);
                stoneGrid.mainColor = new BABYLON.Color3(0.15, 0.15, 0.18);
                stoneGrid.lineColor = new BABYLON.Color3(0.3, 0.3, 0.35);
                stoneGrid.gridRatio = 2.5;
                stoneGrid.majorUnitFrequency = 5;
                stoneGrid.opacity = 0.98;
                ground.material = stoneGrid;
                this.log("Grid Material Loaded", "lime");
            } else {
                // Fallback to Procedural Brick (Stone Plaza look)
                var groundMat = new BABYLON.StandardMaterial("groundMat", this.scene);
                if (BABYLON.BrickProceduralTexture) {
                    var stoneTex = new BABYLON.BrickProceduralTexture("stoneTex", 512, this.scene);
                    stoneTex.numberOfBricksHeight = 20;
                    stoneTex.numberOfBricksWidth = 20;
                    stoneTex.jointColor = new BABYLON.Color3(0.1, 0.1, 0.1);
                    stoneTex.brickColor = new BABYLON.Color3(0.15, 0.15, 0.18);
                    stoneTex.uScale = 50;
                    stoneTex.vScale = 50;
                    groundMat.diffuseTexture = stoneTex;
                    this.log("Procedural Brick Fallback Active", "lime");
                } else {
                    groundMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.18);
                    this.log("All High-Fidelity Materials MISSING", "orange");
                }
                ground.material = groundMat;
            }
            ground.receiveShadows = true;

            this.setupInput();

            this.engine.runRenderLoop(() => { this.scene.render(); this.updateAnimations(); });
            window.addEventListener("resize", () => { this.engine.resize(); });

            this.log("StoneHammer v2.7 Online", "lime");
        } catch (err) {
            this.log("CRITICAL ERR: " + err.message, "red");
        }
    },

    setupInput: function () {
        this.inputMap = {};
        window.addEventListener("keydown", (evt) => {
            this.inputMap[evt.key.toLowerCase()] = true;
        });
        window.addEventListener("keyup", (evt) => {
            this.inputMap[evt.key.toLowerCase()] = false;
        });
        this.log("Input management initialized", "cyan");
    },

    createBackgroundBuildings: function () {
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
    },

    parseVec3: function (data, defaultVal = { x: 0, y: 0, z: 0 }) {
        if (!data) return new BABYLON.Vector3(defaultVal.x, defaultVal.y, defaultVal.z);
        if (Array.isArray(data)) return new BABYLON.Vector3(data[0] ?? defaultVal.x, data[1] ?? defaultVal.y, data[2] ?? defaultVal.z);
        if (typeof data === 'object') return new BABYLON.Vector3(data.x ?? data.X ?? defaultVal.x, data.y ?? data.Y ?? defaultVal.y, data.z ?? data.Z ?? defaultVal.z);
        return new BABYLON.Vector3(defaultVal.x, defaultVal.y, defaultVal.z);
    },

    createMaterial: function (id, config) {
        const mat = new BABYLON.PBRMaterial("mat_" + id, this.scene);
        const colorHex = this.getProp(config, "ColorHex") || "#888888";
        const color = BABYLON.Color3.FromHexString(colorHex);
        const type = (this.getProp(config, "Material") || "Plastic").toLowerCase();

        mat.albedoColor = color;
        if (this.scene.environmentTexture) mat.reflectionTexture = this.scene.environmentTexture;

        if (type.includes("metal")) {
            mat.metallic = 1.0;
            mat.roughness = 0.1;
        } else if (type.includes("glass")) {
            mat.metallic = 0.1;
            mat.roughness = 0.05;
            mat.alpha = 0.3;
            mat.transparencyMode = BABYLON.PBRMaterial.PBRMATERIAL_ALPHABLEND;
        } else if (type.includes("glow")) {
            mat.emissiveColor = color;
            mat.emissiveIntensity = 2.0;
        } else {
            mat.metallic = 0.0;
            mat.roughness = 0.7;
        }
        return mat;
    },



    generateHumanoidParts: function (colors) {
        const skin = this.getProp(colors, "Skin") || "#F1C27D";
        const cloth = this.getProp(colors, "Cloth") || "#4E342E";
        const accent = this.getProp(colors, "Accent") || "#3E2723";
        const charcoal = "#333333";
        const wood = "#5D4037";

        return [
            // Body parts
            { id: "torso", shape: "Box", scale: [0.8, 1.2, 0.4], pos: [0, 1.6, 0], color: cloth },
            { id: "head", shape: "Box", scale: [0.5, 0.5, 0.5], pos: [0, 2.4, 0], color: skin },
            { id: "arm_l", shape: "Box", scale: [0.25, 1.0, 0.25], pos: [-0.6, 1.6, 0], color: skin },
            { id: "arm_r", shape: "Box", scale: [0.25, 1.0, 0.25], pos: [0.6, 1.6, 0], color: skin },
            { id: "leg_l", shape: "Box", scale: [0.3, 1.0, 0.3], pos: [-0.25, 0.5, 0], color: accent },
            { id: "leg_r", shape: "Box", scale: [0.3, 1.0, 0.3], pos: [0.25, 0.5, 0], color: accent },

            // Master Mason Equipment
            { id: "helmet", shape: "Box", scale: [0.6, 0.2, 0.6], pos: [0, 2.7, 0], color: charcoal }, // Protective helmet
            { id: "hammer_handle", shape: "Box", scale: [0.1, 0.8, 0.1], pos: [0.8, 1.6, 0.4], color: wood }, // Handle
            { id: "hammer_head", shape: "Box", scale: [0.4, 0.4, 0.8], pos: [0.8, 2.0, 0.4], color: charcoal } // Stone head
        ];
    },

    spawnVoxel: function (asset, name, isPlayer, transform) {
        const group = new BABYLON.TransformNode("voxel_" + name, this.scene);
        const parts = (asset.parts && asset.parts.length > 0) ? asset.parts : this.generateHumanoidParts(asset.proceduralColors);

        if (!parts || parts.length === 0) {
            this.log("WARNING: No parts generated for " + name, "orange");
        }

        parts.forEach(p => {
            const mesh = BABYLON.MeshBuilder.CreateBox(name + "_" + p.id, {
                width: p.scale[0],
                height: p.scale[1],
                depth: p.scale[2]
            }, this.scene);
            mesh.position = new BABYLON.Vector3(p.pos[0], p.pos[1], p.pos[2]);
            mesh.parent = group;

            const mat = new BABYLON.StandardMaterial("mat_" + name + "_" + p.id, this.scene);
            mat.diffuseColor = BABYLON.Color3.FromHexString(p.color || "#FFFFFF");
            mesh.material = mat;
        });

        const pos = this.parseVec3(this.getProp(transform, "Position"));
        group.position = pos;

        if (isPlayer) {
            this.player = group;
            // Robust Camera Tracking: Focus on torso, not feet
            const camTarget = new BABYLON.TransformNode("camTarget", this.scene);
            camTarget.position = new BABYLON.Vector3(0, 1.5, 0);
            camTarget.parent = this.player;
            this.camera.lockedTarget = camTarget;
            this.log("Player character registered and camera locked to torso", "cyan");
        }

        this.log("Spawned Voxel Actor: " + name, "lime");
    },

    spawnRecipe: function (asset, name, transform) {
        const group = new BABYLON.TransformNode("recipe_" + name, this.scene);
        asset.parts.forEach(p => {
            const mesh = BABYLON.MeshBuilder.CreateBox(name + "_" + p.id, {
                width: (p.scale && p.scale[0]) || 1,
                height: (p.scale && p.scale[1]) || 1,
                depth: (p.scale && p.scale[2]) || 1
            }, this.scene);

            mesh.position = this.parseVec3(p.position);
            if (p.rotation) mesh.rotation = new BABYLON.Vector3(
                BABYLON.Tools.ToRadians(p.rotation[0]),
                BABYLON.Tools.ToRadians(p.rotation[1]),
                BABYLON.Tools.ToRadians(p.rotation[2])
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

        const pos = this.parseVec3(this.getProp(transform, "Position"));
        group.position = pos;
        this.log("Spawned Procedural Landmark: " + name, "lime");
    },

    updateAnimations: function () {
        if (!this.player) return;

        // Rotation: A/D or Left/Right arrows rotate the character
        if (this.inputMap["a"] || this.inputMap["arrowleft"]) {
            this.player.rotation.y -= this.playerRotationSpeed;
        }
        if (this.inputMap["d"] || this.inputMap["arrowright"]) {
            this.player.rotation.y += this.playerRotationSpeed;
        }

        // Movement: W/S or Up/Down arrows move forward/backward along character's forward vector
        let moveDir = BABYLON.Vector3.Zero();
        let moved = false;

        if (this.inputMap["w"] || this.inputMap["arrowup"]) {
            // Get local forward vector
            const forward = this.player.forward.clone();
            forward.y = 0;
            forward.normalize();
            moveDir.addInPlace(forward);
            moved = true;
        }
        if (this.inputMap["s"] || this.inputMap["arrowdown"]) {
            const forward = this.player.forward.clone();
            forward.y = 0;
            forward.normalize();
            moveDir.subtractInPlace(forward);
            moved = true;
        }

        if (moved) {
            moveDir.normalize();
            this.player.position.addInPlace(moveDir.scale(this.playerSpeed));
        }
    },

    clearAll: function () {
        this.scene.meshes.forEach(m => {
            if (m.name !== "ground") m.dispose();
        });
        this.scene.transformNodes.forEach(t => t.dispose());
        this.log("Scene Cleared", "orange");
    }
};
