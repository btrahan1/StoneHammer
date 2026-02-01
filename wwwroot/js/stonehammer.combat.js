/**
 * StoneHammer v20.4: Combat UI & Camera Module
 * Handles UI tracking, Camera positioning, and Floating Text.
 * NOTE: Animation logic has been moved to stonehammer.animator.js to prevent conflicts.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.startUITracking = function (modelIds) {
        if (this._uiObserver) {
            this.scene.onBeforeRenderObservable.remove(this._uiObserver);
        }

        this._uiObserver = this.scene.onBeforeRenderObservable.add(() => {
            if (!modelIds) return;

            modelIds.forEach(id => {
                // Find Mesh
                const mesh = this.scene.getMeshByName("voxel_" + id) ||
                    this.scene.getTransformNodeByName("voxel_" + id) ||
                    this.scene.getMeshByName(id) ||
                    this.scene.getTransformNodeByName(id);

                const uiElement = document.getElementById("ui-" + id);

                if (mesh && uiElement) {
                    uiElement.style.display = "flex";

                    // Project Position (Head offset: +2.8y)
                    const worldPos = mesh.position.clone().add(new BABYLON.Vector3(0, 2.8, 0));
                    const screenPos = BABYLON.Vector3.Project(
                        worldPos,
                        BABYLON.Matrix.Identity(),
                        this.scene.getTransformMatrix(),
                        this.camera.viewport.toGlobal(this.engine.getRenderWidth(), this.engine.getRenderHeight())
                    );

                    uiElement.style.left = screenPos.x + "px";
                    uiElement.style.top = screenPos.y + "px";
                }
            });
        });
    };

    sh.rotateCameraToBattle = function (targetName) {
        // Find the target
        const target = this.scene.getMeshByName("voxel_" + targetName) ||
            this.scene.getTransformNodeByName("voxel_" + targetName) ||
            this.scene.getMeshByName(targetName) ||
            this.scene.getTransformNodeByName(targetName);

        if (target && this.camera) {
            // Dynamic "Over The Shoulder" View
            const player = this.player || this.scene.getTransformNodeByName("voxel_Player");

            if (player && target) {
                const playerPos = player.position;
                const targetPos = target.position;

                // Look at the enemy
                const camTarget = new BABYLON.TransformNode("battleTarget", this.scene);
                camTarget.position = targetPos.clone();
                camTarget.position.y += 1.5;

                this.camera.setTarget(camTarget.position);

                // Calculate angle to place camera behind player
                const dir = playerPos.subtract(targetPos).normalize();
                const alpha = Math.atan2(dir.z, dir.x);

                // Align camera behind player
                this.camera.alpha = alpha - 0.2;
                this.camera.beta = 1.4;
                this.camera.radius = 12;
            } else {
                this.camera.setTarget(target.position);
                this.camera.radius = 10;
                this.camera.beta = 1.3;
            }
        }
    };

    sh.showFloatingText = function (text, position, color) {
        var plane = BABYLON.MeshBuilder.CreatePlane("popup", { size: 2 }, this.scene);
        plane.billboardMode = BABYLON.Mesh.BILLBOARDMODE_ALL;
        plane.position = position.clone().add(new BABYLON.Vector3(0, 2, 0));

        var dynamicTexture = new BABYLON.DynamicTexture("dynamic texture", { width: 256, height: 128 }, this.scene, true);
        dynamicTexture.hasAlpha = true;

        var material = new BABYLON.StandardMaterial("mat", this.scene);
        material.diffuseTexture = dynamicTexture;
        material.specularColor = new BABYLON.Color3(0, 0, 0);
        material.emissiveColor = new BABYLON.Color3(1, 1, 1);
        material.backFaceCulling = false;
        plane.material = material;

        var font = "bold 60px Arial";
        dynamicTexture.drawText(text, null, 80, font, color || "yellow", "transparent", true);

        // Float Up Animation
        var anim = new BABYLON.Animation("float", "position.y", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        var keys = [
            { frame: 0, value: plane.position.y },
            { frame: 30, value: plane.position.y + 2 }
        ];
        anim.setKeys(keys);

        this.scene.beginDirectAnimation(plane, [anim], 0, 30, false, 1, () => {
            plane.dispose();
        });
    };

    sh.removeActor = function (actorName) {
        const root = this.scene.getTransformNodeByName("voxel_" + actorName) ||
            this.scene.getMeshByName("voxel_" + actorName) ||
            this.scene.getTransformNodeByName(actorName) ||
            this.scene.getMeshByName(actorName);
        if (root) {
            root.dispose();
            this.log(actorName + " defeated.", "gray");
        }
    };
})();
