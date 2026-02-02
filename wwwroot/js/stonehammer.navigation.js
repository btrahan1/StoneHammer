/**
 * StoneHammer v10.6: Navigation Module
 * Teleportation hub, building transitions, and scene cleanup.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    // v26.0: Split Entry Logic for Confirmation
    sh.enterBuilding = function (buildingName) {
        // Check for dungeons that need confirmation (Case Insensitive)
        const lowerName = buildingName.toLowerCase();
        if (lowerName.includes("crypt") ||
            lowerName.includes("goblincave") ||
            lowerName.includes("sewer") ||
            lowerName.includes("desert") ||
            lowerName.includes("dungeonentrance")) {

            this.log("Requesting Access: " + buildingName, "cyan");
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('ConfirmEnterBuilding', buildingName);
            }
            return;
        }

        // Direct Entry
        this.commitEntry(buildingName);
    };



    sh.commitEntry = function (buildingName) {
        this.log("Entering: " + buildingName, "cyan");
        this.currentBuilding = buildingName;

        if (this.player) {
            this.lastTownPosition = this.player.position.clone();
            // v25.0+: Push back slightly to avoid immediate re-entry on return
            if (this.player.forward) {
                //   var backDir = this.player.forward.clone().normalize().scale(4); 
                //   this.lastTownPosition.subtractInPlace(backDir);
                // Handled in exit logic better? No, saving LAST position. 
                // We want the return position to be 'safe'.
                // Let's rely on exitBuilding's logic or just save current pos?
                // Actually, saving current pos (at door) is fine if exitBuilding applies offset.
            }
        } else {
            this.lastTownPosition = new BABYLON.Vector3(0, 0, 0);
        }

        if (this.ground) this.ground.setEnabled(false);
        this.clearAll();

        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('HandleEnterBuilding', buildingName);
        }
        this.log("Skybox Active. 'Esc' to exit.", "yellow");
    };

    sh.pushPlayerBack = function () {
        if (this.player && this.player.forward) {
            var backDir = this.player.forward.clone().normalize().scale(-2); // Move backward 2 units
            this.player.position.addInPlace(backDir);
        }
    };

    sh.exitBuilding = function () {
        if (!this.currentBuilding) return;
        this.log("Exiting to Town Atmosphere...", "cyan");
        this.currentBuilding = null;

        // v10.6: Prevent immediate re-entry bounce
        this.triggerLockout = true;
        setTimeout(() => {
            this.triggerLockout = false;
            this.log("Building triggers re-enabled", "gray");
        }, 2000);

        this.clearAll();

        if (this.ground) this.ground.setEnabled(true);

        if (this.dotNetHelper) {
            // Apply Offset on Exit
            // We want to return to lastTownPosition BUT pushed back.
            // Since we don't have player forward ref (player is gone), we rely on lastTownPosition being correct?
            // Actually, let's modify the lastTownPosition HERE before sending it back?
            // Wait, lastTownPosition was saved at entry (at the door).
            // We need to shift it relative to the door?
            // We don't know door facing here easily. 
            // Better to rely on the C# side or just generic "Z+2" fallback if Vector math fails?
            // Let's trust the unmodified saved pos for now, and rely on `enterBuilding` having saved it safely?
            // Actually, in `commitEntry` I was modifying it. Let's ONLY modify it in `enterBuilding` if strictly needed.
            // Reverting to saving EXACT position, and applying offset on SPAWN (in AssetManager)?
            // AssetManager.ExitBuilding calls SpawnPlayer(x,z).
            // Let's modify the coordinate passed back.

            // Hack: Just add random offset? No.
            // Let's stick to passing exact last pos.
            this.dotNetHelper.invokeMethodAsync('HandleExitBuilding', this.lastTownPosition.x, this.lastTownPosition.z);
        }
    };

    sh.spawnSandboxPillar = function () {
        // Clear existing pillars
        ["Sandbox", "Desert", "Lodge"].forEach(name => {
            const old = this.scene.getMeshByName("pillar_" + name);
            if (old) old.dispose();
        });

        const configs = [
            { name: "Sandbox", color: new BABYLON.Color3(0, 0.5, 1), x: 10 },
            { name: "Desert", color: new BABYLON.Color3(1, 0.8, 0), x: 14 },
            { name: "Lodge", color: new BABYLON.Color3(0.5, 0.25, 0), x: 18 }
        ];

        configs.forEach(c => {
            var pillar = BABYLON.MeshBuilder.CreateCylinder("pillar_" + c.name, { height: 6, diameter: 2 }, this.scene);
            pillar.position = new BABYLON.Vector3(c.x, 3, 10);
            var mat = new BABYLON.StandardMaterial("mat_" + c.name, this.scene);
            mat.emissiveColor = c.color;
            pillar.material = mat;
            pillar.metadata = { target: c.name };
        });

        this.log("Teleport Hub Spawned (3 Pillars)", "lime");
    };

    // v12.3: Interaction Config
    sh.setupClickInteraction = function () {
        // SINGLE CLICK & SMART DOUBLE CLICK HANDLER
        this.scene.onPointerDown = (evt, pickResult) => {
            if (!pickResult.hit || !pickResult.pickedMesh) return;

            let mesh = pickResult.pickedMesh;
            let originalPartName = mesh.name;

            // 1. Walk up hierarchy to find interactable root
            while (mesh.parent &&
                !mesh.name.startsWith("pillar_") &&
                !mesh.name.includes("Exit") &&
                !mesh.name.includes("LodgeExit") &&
                !mesh.name.includes("Loot") &&
                !mesh.name.includes("CryptEntrance") &&
                !mesh.name.toLowerCase().includes("stairs") &&
                !(mesh.metadata && mesh.metadata.isEnemy)) {
                mesh = mesh.parent;
            }
            const name = mesh.name;
            const now = Date.now();

            // 2. SMART DOUBLE CLICK LOGIC
            if (this.lastClickTarget === mesh.uniqueId && (now - this.lastClickTime < 500)) {
                this.log(`Double Click Detected on: ${name} (Part: ${originalPartName})`, "gray");
                this.handleDoubleClick(mesh, name);
                this.lastClickTarget = null;
                this.lastClickTime = 0;
                return;
            }

            // Update Click History
            this.lastClickTarget = mesh.uniqueId;
            this.lastClickTime = now;

            // 3. SINGLE CLICK LOGIC (Existing)
            if (name.startsWith("pillar_")) {
                const target = mesh.metadata?.target;
                if (target) {
                    this.log("Teleporting to " + target + "...", "lime");
                    this.enterBuilding(target);
                }
            }

            if (name.includes("Exit") || name.includes("LodgeExit")) {
                if (name.includes("DesertExit")) {
                    this.log("Escaping the Wasteland...", "cyan");
                    this.exitDesert();
                    if (this.dotNetHelper) this.dotNetHelper.invokeMethodAsync('HandleExitBuilding', 50, 50);
                }
                else {
                    this.log("Returning to Town...", "magenta");
                    this.exitBuilding();
                }
            }

            if (name.toLowerCase().includes("stairsdown") || name.toLowerCase().includes("stairs_down")) {
                let currentDepth = 1;
                if (this.currentBuilding && this.currentBuilding.includes("Depth")) {
                    currentDepth = parseInt(this.currentBuilding.split('_')[2]);
                }
                const nextDepth = currentDepth + 1;
                this.log("Descending to Depth " + nextDepth + "...", "orange");
                this.enterBuilding("Crypt_Depth_" + nextDepth);
            }

            if (name.toLowerCase().includes("stairsup") || name.toLowerCase().includes("stairs_up")) {
                let currentDepth = 2;
                if (this.currentBuilding && this.currentBuilding.includes("Depth")) {
                    currentDepth = parseInt(this.currentBuilding.split('_')[2]);
                }
                const nextDepth = currentDepth - 1;
                if (nextDepth < 1) {
                    this.log("Leaving the Crypt...", "magenta");
                    this.exitBuilding();
                } else {
                    this.log("Ascending to Depth " + nextDepth + "...", "cyan");
                    this.enterBuilding("Crypt_Depth_" + nextDepth);
                }
            }

            if (name.includes("Loot Chest") || name.includes("LootChest")) {
                this.log("Looting...", "yellow");
                if (mesh.parent) mesh.parent.dispose();
                else mesh.dispose();
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OpenLoot');
                }
            }

            if (name.includes("Hammer & Sickle Store") || name.includes("general_store") || name.toLowerCase().includes("store")) {
                this.log("Opening Shop...", "gold");
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OpenShop');
                }
            }
        };
    };

    // Helper to extract double click actions
    sh.handleDoubleClick = function (mesh, name) {
        if (name.includes("CryptEntrance") || name.startsWith("Stairs") || name.includes("GoblinCaveEntrance") || name.includes("SewerEntrance")) {
            if (name.includes("GoblinCaveEntrance")) {
                this.log("Entering the Goblin Cave...", "green");
                this.enterBuilding("GoblinCave");
            } else if (name.includes("SewerEntrance")) {
                this.log("Entering the Sewers...", "lime");
                this.enterBuilding("Sewer");
            } else {
                this.log("Descending into The Crypt...", "red");
                this.enterBuilding(name.includes("Depth") ? name : `Crypt_Depth_${name.endsWith("Down") ? 1 : 0}`);
            }
        }

        if (name.includes("DesertEntrance")) {
            this.log("Warping to the Wasteland...", "orange");
            this.enterBuilding("Desert");
        }

        if (mesh.metadata && mesh.metadata.isEnemy) {
            let actorName = name.replace("voxel_", "").replace("recipe_", "");
            this.log("Engaging " + actorName + "!", "red");
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('StartCombat', actorName, mesh.metadata);
            }
        }

        // v28.0: Generic Metadata Actions
        if (mesh.metadata && mesh.metadata.action) {
            if (mesh.metadata.action === "EnterDungeon") {
                this.log("Entering Portal: " + mesh.metadata.target, "magenta");
                this.enterBuilding(mesh.metadata.target);
            }
        }
    };

    sh.clearAll = function () {
        const meshes = this.scene.meshes;
        for (var i = meshes.length - 1; i >= 0; i--) {
            var m = meshes[i];
            if (m.name !== "skyBox") {
                m.dispose();
            }
        }

        // v26.1: Deep Clean for Performance
        const mats = [...this.scene.materials];
        mats.forEach(m => {
            if (m.name !== "skyBox" && m.name !== "default material") {
                m.dispose(true, true);
            }
        });

        if (this.materialCache) this.materialCache = {};

        this.scene.transformNodes.forEach(t => t.dispose());

        this.player = null;
        this.buildingTriggers = [];
        this.log("Scene Cleared (Deep)", "orange");
    };
})();
