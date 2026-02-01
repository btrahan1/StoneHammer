/**
 * StoneHammer v20.6: Combat Animation Module
 * Dedicated file for combat animations to prevent logic conflicts.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    // DEBUG: Validate file load
    console.log("[Animator] v20.7 Loaded - Right Arm Fix");

    sh.playCombatAnimation = function (actorName, animType) {
        console.log(`[Animator] Requesting Animation '${animType}' for '${actorName}'`);

        // CHECK 1: candidates
        const candidates = this.scene.getNodes().filter(n => n.name === "voxel_" + actorName || n.name === actorName);

        let root = null;
        for (let c of candidates) {
            const children = c.getChildren(null, false);
            const hasBody = children.some(child => child.name.includes("arm") || child.name.includes("leg"));
            if (hasBody) {
                root = c;
                break;
            }
        }
        if (!root && candidates.length > 0) root = candidates[0];

        if (!root) {
            console.warn("[Animator] Actor not found: " + actorName);
            return;
        }

        if (animType === "Attack") {
            console.log(`[Animator] Executing 4-Stage Attack for '${actorName}'`);

            const isHero = root.position.x < 0;
            const direction = isHero ? 1 : -1;
            const startX = root.position.x;
            const targetX = startX + (8.0 * direction);

            // ROOT ANIMATION (Position) - Merged into single track
            const moveAnim = new BABYLON.Animation("attackMove", "position.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            moveAnim.setKeys([
                { frame: 0, value: startX },
                { frame: 30, value: targetX },  // Dash Complete (0.5s)
                { frame: 60, value: targetX },  // Hold for Swing (0.5s)
                { frame: 90, value: startX }    // Return Complete (0.5s)
            ]);

            // ARM ANIMATION (Rotation)
            // v20.7: Right Arm Logic (Stricter)
            let arm = root.getChildren((n) => n.name.includes("arm_r") || n.name.includes("right"), false)[0];
            // Fallback to left only if right is missing
            if (!arm) arm = root.getChildren((n) => n.name.includes("arm_l") || n.name.includes("left"), false)[0];

            let swingAnim = null;
            if (arm) {
                const startRot = arm.rotation.x;
                swingAnim = new BABYLON.Animation("atk", "rotation.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
                swingAnim.setKeys([
                    { frame: 0, value: startRot },
                    { frame: 30, value: startRot },
                    { frame: 45, value: startRot - 2.5 },
                    { frame: 55, value: startRot + 1.5 },
                    { frame: 65, value: startRot }
                ]);
            }

            // EXECUTE (Fixed: uses moveAnim)
            // Forced Slow Speed (0.5) for visibility
            this.scene.beginDirectAnimation(root, [moveAnim], 0, 90, false, 0.5);

            if (arm && swingAnim) {
                this.scene.beginDirectAnimation(arm, [swingAnim], 0, 90, false, 0.5);
            }
        }
        else if (animType === "Hit") {
            // Hit Logic
            root.getChildren(null, true).forEach(mesh => {
                if (mesh.material && mesh.material.diffuseColor) {
                    const oldColor = mesh.material.diffuseColor.clone();
                    mesh.material.diffuseColor = BABYLON.Color3.Red();
                    setTimeout(() => { if (mesh.material) mesh.material.diffuseColor = oldColor; }, 200);
                }
            });
            const startX = root.position.x;
            const anim = new BABYLON.Animation("shake", "position.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            anim.setKeys([
                { frame: 0, value: startX },
                { frame: 5, value: startX + 0.2 },
                { frame: 10, value: startX - 0.2 },
                { frame: 15, value: startX + 0.2 },
                { frame: 20, value: startX }
            ]);
            this.scene.beginDirectAnimation(root, [anim], 0, 20, false);
        }
        else if (animType === "Die") {
            // Die Logic
            const startRot = root.rotation.z;
            const startY = root.position.y;
            const animRot = new BABYLON.Animation("dieRot", "rotation.z", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            animRot.setKeys([{ frame: 0, value: startRot }, { frame: 30, value: startRot + Math.PI / 2 }]);
            const animPos = new BABYLON.Animation("diePos", "position.y", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            animPos.setKeys([{ frame: 0, value: startY }, { frame: 20, value: startY }, { frame: 40, value: startY - 0.5 }]);
            this.scene.beginDirectAnimation(root, [animRot, animPos], 0, 40, false);
        }
    };
})();
