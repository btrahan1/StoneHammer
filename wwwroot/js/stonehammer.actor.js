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

            // v10.7: Jump & Gravity Logic
            const gravity = -0.5;
            const jumpForce = 0.8;

            // Check Grounded State (Simple: If we didn't fall much last frame?) 
            // Better: Raycast down? Or just rely on collisions?
            // For now, let's just use a simple cooldown or check if Y velocity was zeroed out by collision

            if (this.inputMap[" "] && this.player.position.y <= (this.lastY || -100) + 0.1) {
                this.verticalVelocity = jumpForce;
            }

            if (!this.verticalVelocity) this.verticalVelocity = 0;
            this.verticalVelocity += gravity * 0.1; // Apply gravity over time?
            // Actually, moveWithCollisions handles displacement. 
            // We just need to feed it a downward vector.
            // But for jumping, we need persistent vertical velocity.

            // Reset vertical velocity if we hit ground (simple hack: if we are not moving down despite gravity)
            // Let's stick to the manual override for now similar to existing:

            // Simple Jump Implementation:
            if (this.inputMap[" "]) {
                // only jump if close to ground (Y ~ 0 or Bedrock -2 or Room -0.5)
                // This is tricky without raycast. 
                // Let's just allow "flying" effectively for debug/ease of movement for now?
                // No, User asked for "Jump".
                moveDir.y = 1.0;
            } else {
                moveDir.y = -0.5; // Gravity
            }

            // NOTE: True platformer physics requires tracking "isGrounded".
            // For this top-down/RPG view, simple "up if space" might be enough to climb the ledge.

            // REFINED LOGIC:
            if (this.inputMap[" "]) {
                this.isJumping = true;
            }

            if (this.isJumping) {
                moveDir.y = 0.5;
                // Stop jumping after short duration?
                this.jumpTimer = (this.jumpTimer || 0) + 1;
                if (this.jumpTimer > 20) {
                    this.isJumping = false;
                    this.jumpTimer = 0;
                }
            } else {
                moveDir.y = -0.5;
            }

            const velocity = moveDir.scale(currentSpeed);
            this.player.moveWithCollisions(velocity);

            // Re-leveling if not jump support
            // this.player.position.y = ... (Ideally handled by collision floor)

            // v14.0: Desert Terrain Clamping (Override collision if desert active?)
            // If in Desert, we might want to rely on moveWithCollisions against the heightmap mesh
            // BUT if desert uses heightmap probing, we keep it.
            if (this.desert && this.desert.ground) {
                const y = this.desert.getHeightAt(this.player.position.x, this.player.position.z);
                // If collision system fails to find ground, snap to heightmap
                if (this.player.position.y < y + 1.6) {
                    this.player.position.y = y + 1.6;
                }
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
                // v24.0: Guard clause. If entering a building, player might be nulled mid-loop.
                if (!this.player) return;

                const dist = BABYLON.Vector3.Distance(this.player.position, t.pos);
                if (dist < t.radius) {
                    this.enterBuilding(t.name);
                    return; // Stop checking other triggers
                }
            });
        }
    };
})();
