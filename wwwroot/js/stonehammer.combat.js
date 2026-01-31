/**
 * StoneHammer v15.0: Combat Module
 * Handles visual effects for turn-based combat (Animations, Camera, VFX).
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.rotateCameraToBattle = function (targetName) {
        // Find the target: Supports "voxel_Name" or just "Name" if "voxel_" is missing
        const target = this.scene.getMeshByName("voxel_" + targetName) ||
            this.scene.getTransformNodeByName("voxel_" + targetName) ||
            this.scene.getMeshByName(targetName) ||
            this.scene.getTransformNodeByName(targetName);

        if (target && this.camera) {
            // Store old position to return? Maybe not needed for now.
            // Move camera to a dramatic "Third Person Over Shoulder" view relative to player?
            // Or a "Side View" like Final Fantasy?

            // Dynamic "Over The Shoulder" View
            // Assuming Player is the global 'this.player' or we find "voxel_Player"
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
                // Direction from Enemy to Player
                const dir = playerPos.subtract(targetPos).normalize();

                // Manual alpha calculation (ArcRotateCamera uses alpha for horizontal rotation)
                // Babylon alpha 0 is usually +X axis?
                // Math.atan2(z, x)
                const alpha = Math.atan2(dir.z, dir.x);

                // Align camera behind player, slightly offset
                this.camera.alpha = alpha - 0.2; // Slight offset for "over shoulder"
                this.camera.beta = 1.4; // Eye level-ish
                this.camera.radius = 12; // Distance to include player in frame
            } else {
                // Fallback if player not found
                this.camera.setTarget(target.position);
                this.camera.radius = 10;
                this.camera.beta = 1.3;
            }
        }
    };

    sh.playCombatAnimation = function (actorName, animType) {
        console.log(`[Combat] Requesting Animation '${animType}' for '${actorName}'`);

        // v15.4: Smart Root Search (Handle duplicates)
        // Find ALL nodes that match the name
        const candidates = this.scene.getNodes().filter(n =>
            n.name === "voxel_" + actorName ||
            n.name === actorName
        );

        let root = null;

        // Find the one with body parts (arms/legs)
        for (let c of candidates) {
            const children = c.getChildren(null, false); // Check rigid hierarchy
            const hasBody = children.some(child => child.name.includes("arm") || child.name.includes("leg"));
            if (hasBody) {
                root = c;
                console.log(`[Combat] Selected '${c.name}' (ID: ${c.uniqueId}) via BodyCheck.`);
                break;
            }
        }

        // Fallback: If no body found, just take the first candidate (e.g. Slime?)
        if (!root && candidates.length > 0) {
            root = candidates[0];
            console.log(`[Combat] No body parts found. Defaulting to '${root.name}'.`);
        }

        if (!root) {
            console.warn("[Combat] Actor not found for animation: " + actorName);
            // DEBUG: List potential candidates?
            this.scene.transformNodes.forEach(t => {
                if (t.name.includes("voxel")) console.log("Candidate: " + t.name);
            });
            return;
        }

        if (animType === "Attack") {
            // Try to find the arm node - Recursive search (false) to be safe
            let arm = root.getChildren((n) => n.name.includes("arm_r"), false)[0];
            if (!arm) {
                arm = root.getChildren((n) => n.name.includes("arm_l"), false)[0];
            }

            if (arm) {
                console.log(`[Combat] Found Arm '${arm.name}', animating...`);
                // Simple swing tween
                const startRot = arm.rotation.x;
                const animation = new BABYLON.Animation("atk", "rotation.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
                const keys = [
                    { frame: 0, value: startRot },
                    { frame: 10, value: startRot - 1.5 }, // Swing up/back
                    { frame: 20, value: startRot + 1.0 }, // Smash down
                    { frame: 40, value: startRot } // Return
                ];
                animation.setKeys(keys);
                this.scene.beginDirectAnimation(arm, [animation], 0, 40, false);
            } else {
                console.warn(`[Combat] Arm not found on '${root.name}'. Attempting body lunge.`);
                console.log("Children found on root:");
                root.getChildren(null, false).forEach(c => console.log(" - " + c.name));

                // Fallback: Lunge the whole body
                const startZ = root.position.z;
                const lungeDir = (actorName === "Player") ? 1 : -1; // Assuming Z-axis alignment
                const animation = new BABYLON.Animation("lunge", "position.z", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
                const keys = [
                    { frame: 0, value: startZ },
                    { frame: 10, value: startZ + (1.5 * lungeDir) },
                    { frame: 30, value: startZ }
                ];
                animation.setKeys(keys);
                this.scene.beginDirectAnimation(root, [animation], 0, 30, false);
            }
        }
        else if (animType === "Hit") {
            // Flash Red
            root.getChildren(null, true).forEach(mesh => {
                if (mesh.material && mesh.material.diffuseColor) {
                    const oldColor = mesh.material.diffuseColor.clone();
                    mesh.material.diffuseColor = BABYLON.Color3.Red();
                    setTimeout(() => {
                        if (mesh.material) mesh.material.diffuseColor = oldColor;
                    }, 200);
                }
            });

            // Shake
            const startX = root.position.x;
            const anim = new BABYLON.Animation("shake", "position.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            const keys = [
                { frame: 0, value: startX },
                { frame: 5, value: startX + 0.2 },
                { frame: 10, value: startX - 0.2 },
                { frame: 15, value: startX + 0.2 },
                { frame: 20, value: startX }
            ];
            anim.setKeys(keys);
            this.scene.beginDirectAnimation(root, [anim], 0, 20, false);
        }
        else if (animType === "Die") {
            // Topple over
            const startRot = root.rotation.x;
            const anim = new BABYLON.Animation("die", "rotation.x", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            const keys = [
                { frame: 0, value: startRot },
                { frame: 30, value: startRot + Math.PI / 2 } // Fall backward (or forward depending on orient)
            ];
            anim.setKeys(keys);
            this.scene.beginDirectAnimation(root, [anim], 0, 30, false, 1.0, () => {
                // Fade out?
            });
        }
    };

    sh.showFloatingText = function (text, position, color) {
        // Simple HTML overlay or 3D text? HTML is easier for "popup" feel over canvas.
        // But 3D text stays in world. Let's do a quick GUI DynamicTexture plane.

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

        // Scale Out
        // var anim2 ...

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
