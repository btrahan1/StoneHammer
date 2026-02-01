/**
 * StoneHammer v10.6: Navigation Module
 * Teleportation hub, building transitions, and scene cleanup.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.enterBuilding = function (buildingName) {
        this.log("Entering: " + buildingName, "cyan");
        this.currentBuilding = buildingName;

        if (this.player) {
            this.lastTownPosition = this.player.position.clone();
            this.lastTownPosition.z += 2;
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

    // v12.3: Double Click for Crypt
    sh.setupClickInteraction = function () {
        this.scene.onPointerObservable.add((pointerInfo) => {
            if (pointerInfo.type === BABYLON.PointerEventTypes.POINTERDOUBLETAP) {
                if (pointerInfo.pickInfo.hit && pointerInfo.pickInfo.pickedMesh) {
                    let mesh = pointerInfo.pickInfo.pickedMesh;

                    // Walk up hierarchy to find interactable root
                    while (mesh.parent && !mesh.name.includes("CryptEntrance") && !mesh.name.includes("Stairs") && !mesh.name.includes("Skeleton") && !mesh.name.includes("Loot") && !mesh.name.includes("Exit")) {
                        mesh = mesh.parent;
                    }
                    const name = mesh.name;

                    if (name.includes("CryptEntrance") || name.startsWith("Stairs")) {
                        this.log("Descending into The Crypt...", "red");
                        // Pass the building name, which might include depth for dungeons
                        this.enterBuilding(name.includes("Depth") ? name : `Crypt_Depth_${name.endsWith("Down") ? 1 : 0}`);
                    }

                    // v14.0: Desert Entrance
                    if (name.includes("DesertEntrance")) {
                        this.log("Warping to the Wasteland...", "orange");
                        this.enterBuilding("Desert");
                    }

                    // v15.0: Combat Trigger (Skeleton Clicks)
                    if (name.includes("Skeleton")) {
                        // Find the root actor name
                        // Already walked up? Maybe not if Voxel structure is complex.
                        // Let's ensure we strip voxel_ prefix if present.
                        let actorName = name.replace("voxel_", "");
                        // Removing trailing parts like _A _B handled by StartCombat?
                        // Actually the ID is usually "Skeleton_Lvl1_A".

                        this.log("Engaging " + actorName + "!", "red");
                        if (this.dotNetHelper) {
                            this.dotNetHelper.invokeMethodAsync('StartCombat', actorName);
                        }
                    }

                    // v15.1: Loot Chest Interaction
                    if (name.includes("Loot Chest") || name.includes("LootChest")) {
                        this.log("Looting...", "yellow");
                        const chest = mesh; // Use likely root

                        if (chest.parent) chest.parent.dispose();
                        else chest.dispose();

                        if (this.dotNetHelper) {
                            this.dotNetHelper.invokeMethodAsync('OpenLoot');
                        }
                    }
                }
            }
        });

        // SINGLE CLICK HANDLER UPDATE
        this.scene.onPointerDown = (evt, pickResult) => {
            if (!pickResult.hit || !pickResult.pickedMesh) return;

            let mesh = pickResult.pickedMesh;

            // Walk up for basic interactions
            // Fix: Check lowercase for case-insensitive matching
            while (mesh.parent &&
                !mesh.name.startsWith("pillar_") &&
                !mesh.name.includes("Exit") &&
                !mesh.name.includes("LodgeExit") &&
                !mesh.name.includes("Loot") &&
                !mesh.name.toLowerCase().includes("stairs")) {
                mesh = mesh.parent;
            }
            const name = mesh.name;

            // v9.20: Teleport Hub Pillars
            if (name.startsWith("pillar_")) {
                const target = mesh.metadata?.target;
                if (target) {
                    this.log("Teleporting to " + target + "...", "lime");
                    this.enterBuilding(target);
                }
            }

            // v10.1: Exit Crystal (Single Click) - Supports "ExitCrystal", "LodgeExit", "CryptExit"
            if (name.includes("Exit") || name.includes("LodgeExit")) { // "Exit" covers ExitCrystal, CryptExit, DesertExit
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

            // v13.0: Dungeon Navigation
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

            // v19.1: Loot Chest (Single Click)
            if (name.includes("Loot Chest") || name.includes("LootChest")) {
                this.log("Looting...", "yellow");

                // Dispose visual chest
                if (mesh.parent) mesh.parent.dispose();
                else mesh.dispose();

                // Trigger UI
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OpenLoot');
                }
            }

            // v21.0: Store Interaction
            // Check for any part of the store (e.g. wall, door, sign)
            if (name.includes("Hammer & Sickle Store") || name.includes("general_store") || name.toLowerCase().includes("store")) {
                this.log("Opening Shop...", "gold");
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OpenShop');
                }
            }
        };
    };

    sh.clearAll = function () {
        const meshes = this.scene.meshes;
        for (var i = meshes.length - 1; i >= 0; i--) {
            var m = meshes[i];
            // v10.1: Skybox persists, everything else (including JSON ground) clears
            if (m.name !== "skyBox") {
                m.dispose();
            }
        }
        this.scene.transformNodes.forEach(t => t.dispose());
        this.player = null;
        this.buildingTriggers = [];
        this.log("Scene Cleared", "orange");
    };
})();
