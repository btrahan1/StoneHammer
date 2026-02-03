/**
 * StoneHammer v35.0: AI Module
 * Simple NPC behaviors (Patrolling, etc.)
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.patrollers = [];

    sh.initAI = function () {
        if (this.aiInitialized) return;
        this.aiInitialized = true;
        this.log("AI System Initialized", "cyan");

        this.scene.registerBeforeRender(() => {
            this.updateAI();
        });
    };

    sh.registerPatrol = function (actorName, path, speed) {
        if (!path || path.length < 2) return;

        // Convert array-of-arrays to Vector3s if needed
        const points = path.map(p => new BABYLON.Vector3(p[0], 0, p[2] || p[1]));
        // Note: Input path usually [x, z] or [x, y, z]? Checking C# model. Likely [x, y, z].
        // If Y is missing or flattened, we handle it.

        this.patrollers.push({
            name: actorName,
            path: points,
            speed: speed || 2.0,
            index: 0,
            wait: 0
        });

        this.log(`Patrol Registered: ${actorName} (${points.length} pts)`, "gray");
    };

    sh.updateAI = function () {
        if (!this.patrollers.length) return;
        const dt = this.scene.getEngine().getDeltaTime() / 1000;

        this.patrollers.forEach(p => {
            // Try exact name first, then voxel prefix
            let mesh = this.scene.getMeshByName(p.name) || this.scene.getTransformNodeByName(p.name);
            if (!mesh) {
                mesh = this.scene.getMeshByName("voxel_" + p.name) || this.scene.getTransformNodeByName("voxel_" + p.name);
            }
            if (!mesh) return; // Mesh might not be loaded yet or destroyed

            // Logic: Move to Target
            const target = p.path[p.index];
            const currentPos = mesh.position;

            // Ignore Y for distance check (2D navigation)
            const dist = Math.sqrt(Math.pow(target.x - currentPos.x, 2) + Math.pow(target.z - currentPos.z, 2));

            if (dist < 0.5) {
                // Arrived
                p.index = (p.index + 1) % p.path.length;
                // Optional: Wait time?
            } else {
                // Move
                const dir = target.subtract(currentPos);
                dir.y = 0;
                dir.normalize();

                const moveVec = dir.scale(p.speed * dt);
                mesh.position.addInPlace(moveVec);

                // Rotate to face
                mesh.lookAt(new BABYLON.Vector3(target.x, mesh.position.y, target.z), 0);

                // Animation: Walk Cycle
                p.walkTime = (p.walkTime || 0) + (dt * p.speed * 5); // Speed factor
                const swing = Math.sin(p.walkTime) * 0.5;

                mesh.getChildren().forEach(node => {
                    const name = node.name.toLowerCase();
                    if (name.includes("arm_l") || name.includes("leg_r")) {
                        node.rotation.x = swing;
                    } else if (name.includes("arm_r") || name.includes("leg_l")) {
                        node.rotation.x = -swing;
                    }
                });
            }
        });
    };

})();
