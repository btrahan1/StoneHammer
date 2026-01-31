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

    sh.setupClickInteraction = function () {
        this.scene.onPointerDown = (evt, pickResult) => {
            if (!pickResult.hit || !pickResult.pickedMesh) return;
            const name = pickResult.pickedMesh.name;

            // v9.20: Teleport Hub Pillars
            if (name.startsWith("pillar_")) {
                const target = pickResult.pickedMesh.metadata.target;
                this.log("Teleporting to " + target + "...", "lime");
                this.enterBuilding(target);
            }

            // v10.1: Exit Crystal (Single Click)
            if (name.includes("ExitCrystal")) {
                this.log("Returning to Town...", "magenta");
                this.exitBuilding();
            }
        };

        // v12.3: Double Click for Crypt
        this.scene.onPointerObservable.add((pointerInfo) => {
            if (pointerInfo.type === BABYLON.PointerEventTypes.POINTERDOUBLETAP) {
                if (pointerInfo.pickInfo.hit && pointerInfo.pickInfo.pickedMesh) {
                    const name = pointerInfo.pickInfo.pickedMesh.name;
                    if (name.includes("CryptEntrance")) {
                        this.log("Descending into The Crypt...", "red");
                        this.enterBuilding("Crypt");
                    }
                }
            }
        });
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
