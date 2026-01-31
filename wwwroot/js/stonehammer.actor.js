/**
 * StoneHammer v10.0: Actor Module
 * Player movement, sprinting, and procedural walk cycle animations.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.updateAnimations = function () {
        if (!this.player) return;

        // Character Rotation
        if (this.inputMap["a"] || this.inputMap["arrowleft"]) {
            this.player.rotation.y -= this.playerRotationSpeed;
        }
        if (this.inputMap["d"] || this.inputMap["arrowright"]) {
            this.player.rotation.y += this.playerRotationSpeed;
        }

        // Movement Vector Calculation
        let moveDir = BABYLON.Vector3.Zero();
        let moved = false;

        if (this.inputMap["w"] || this.inputMap["arrowup"]) {
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
            const isSprinting = this.inputMap["shift"];
            const currentSpeed = isSprinting ? 0.5 : 0.25;
            this.walkTime += isSprinting ? 0.25 : 0.15;

            moveDir.normalize();
            this.player.position.addInPlace(moveDir.scale(currentSpeed));

            // v14.0: Terrain Clamping
            if (this.desert && this.desert.ground) {
                const y = this.desert.getHeightAt(this.player.position.x, this.player.position.z);
                this.player.position.y = y + 1.6; // Keep feet on ground (approx 1.6 units to eyes/center)
            }
        } else {
            this.walkTime *= 0.8;
            if (this.walkTime < 0.01) this.walkTime = 0;
        }

        // Procedural Sine-Wave Walk Cycle
        const armSwing = Math.sin(this.walkTime) * 0.5;
        this.player.getChildren().forEach(node => {
            if (node.name.includes("arm_l") || node.name.includes("leg_r")) {
                node.rotation.x = armSwing;
            } else if (node.name.includes("arm_r") || node.name.includes("leg_l")) {
                node.rotation.x = -armSwing;
            }
        });

        // v10.6: Building Trigger Detection (With Lockout)
        if (!this.currentBuilding && !this.triggerLockout) {
            this.buildingTriggers.forEach(t => {
                const dist = BABYLON.Vector3.Distance(this.player.position, t.pos);
                if (dist < t.radius) {
                    this.enterBuilding(t.name);
                }
            });
        }
    };
})();
