/**
 * StoneHammer v20.6: Combat Animation Module
 * Dedicated file for combat animations to prevent logic conflicts.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    // DEBUG: Validate file load
    console.log("[Animator] v20.7 Loaded - Right Arm Fix");

    sh.playCombatAnimation = function (actorName, animType, targetId) {
        console.log(`[Animator] Requesting Animation '${animType}' for '${actorName}' -> '${targetId || "None"}'`);

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

        if (animType === "Attack" || animType === "Attack_Heavy" || animType === "Attack_Cleave" || animType === "Spin") {
            // ... (Melee Logic Omitted for Brevity - It remains unchanged because it has built-in 'dash to target' logic that we might need to parameterize too, but for now focusing on Ranged)
            // Actually, Melee logic hardcodes "8.0 units forward". It ignores TargetID. 
            // If we want melee to go TO the target, we'd need to update it too. but user only complained about Arrows.

            // [Re-inserting existing Melee logic for safety]
            console.log(`[Animator] Melee Attack for '${actorName}'`);
            const isHero = root.position.x < 0;
            const direction = isHero ? 1 : -1;
            const startX = root.position.x;
            const targetX = startX + (8.0 * direction);

            const moveAnim = new BABYLON.Animation("attackMove", "position.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            moveAnim.setKeys([
                { frame: 0, value: startX },
                { frame: 30, value: targetX },
                { frame: 60, value: targetX },
                { frame: 90, value: startX }
            ]);

            // Swing
            let arm = root.getChildren((n) => n.name.includes("arm_r") || n.name.includes("right"), false)[0];
            if (!arm) arm = root.getChildren((n) => n.name.includes("arm_l") || n.name.includes("left"), false)[0];

            const swingAnim = new BABYLON.Animation("atk", "rotation.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            if (arm) {
                const startRot = arm.rotation.x;
                swingAnim.setKeys([
                    { frame: 0, value: startRot },
                    { frame: 30, value: startRot },
                    { frame: 45, value: startRot - 2.5 },
                    { frame: 55, value: startRot + 1.5 },
                    { frame: 65, value: startRot }
                ]);
                this.scene.beginDirectAnimation(arm, [swingAnim], 0, 90, false, 0.5);
            }
            this.scene.beginDirectAnimation(root, [moveAnim], 0, 90, false, 0.5);
        }
        else if (animType === "Shoot" || animType.startsWith("Cast_")) {
            // RANGED / MAGIC LOGIC
            console.log(`[Animator] Ranged/Magic: ${animType}`);
            const isHero = root.position.x < 0;
            const direction = isHero ? 1 : -1;
            const startX = root.position.x;
            const stepX = startX + (2.0 * direction); // Small step

            // 1. Movement (Step, Hold, Back)
            const moveAnim = new BABYLON.Animation("stepMove", "position.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            moveAnim.setKeys([
                { frame: 0, value: startX },
                { frame: 20, value: stepX },
                { frame: 60, value: stepX }, // Hold longer for proj
                { frame: 80, value: startX }
            ]);

            this.scene.beginDirectAnimation(root, [moveAnim], 0, 80, false, 1.0);

            // 2. Projectile (Delayed to sync with step)
            setTimeout(() => {
                // Identify Target by ID if provided
                let targetNode = null;
                if (targetId) {
                    targetNode = this.scene.getTransformNodeByName("voxel_" + targetId) || this.scene.getMeshByName("voxel_" + targetId);
                    // Try without voxel prefix if direct ID passed
                    if (!targetNode) targetNode = this.scene.getTransformNodeByName(targetId) || this.scene.getMeshByName(targetId);
                }

                // Fallback Logic
                if (!targetNode) {
                    // v27.4: Metadata-Driven Targeting
                    const targetFaction = isHero ? "Enemy" : "Hero";

                    const targets = this.scene.getNodes().filter(n => {
                        if (!n.metadata || n.metadata.faction !== targetFaction) return false;
                        if (n.isEnabled() === false) return false;
                        return true;
                    });

                    // Legacy Fallback (temporary safety net)
                    if (targets.length === 0) {
                        const legacyName = isHero ? "Skeleton" : "Mason";
                        const legacyTargets = this.scene.getNodes().filter(n => n.name.includes(legacyName) && n.isEnabled() !== false);
                        if (legacyTargets.length > 0) targetNode = legacyTargets[0];
                    } else {
                        targetNode = targets[0];
                    }
                }

                const targetPos = targetNode ? targetNode.position.clone() : new BABYLON.Vector3(startX + (15 * direction), 2, 0);

                if (animType === "Cast_Smite") {
                    sh.spawnSmiteEffect(targetPos);
                } else {
                    let color = BABYLON.Color3.White();
                    let isSpell = false;

                    if (animType === "Cast_Fire") { color = BABYLON.Color3.Red(); isSpell = true; }
                    if (animType === "Cast_Ice") { color = BABYLON.Color3.Teal(); isSpell = true; }
                    if (animType === "Shoot") color = new BABYLON.Color3(0.6, 0.4, 0.2); // Wood

                    // Spawn Projectile
                    sh.spawnProjectile(root.position.add(new BABYLON.Vector3(0, 3, 0)), targetPos, color, animType === "Shoot", isSpell);
                }

                // Trigger HIT Anim on target slightly later
                if (targetNode) {
                    setTimeout(() => {
                        // Flashing red handled by CombatService sending "Hit" anim? 
                        // Or we can force a flash here. 
                        // Let's rely on the separate "Hit" call from C# for damage numbers/reaction, 
                        // BUT for visuals, we might want the explosion now.
                    }, 500);
                }

            }, 400); // Wait for step
        }
        else if (animType === "Hit") {
            // Hit Logic (Keep existing)
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
            // Die Logic (Keep existing)
            const startRot = root.rotation.z;
            const startY = root.position.y;
            const animRot = new BABYLON.Animation("dieRot", "rotation.z", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            animRot.setKeys([{ frame: 0, value: startRot }, { frame: 30, value: startRot + Math.PI / 2 }]);
            const animPos = new BABYLON.Animation("diePos", "position.y", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
            animPos.setKeys([{ frame: 0, value: startY }, { frame: 20, value: startY }, { frame: 40, value: startY - 0.5 }]);
            this.scene.beginDirectAnimation(root, [animRot, animPos], 0, 40, false);
        }

    };

    // HELPER: Projectile
    sh.spawnProjectile = function (start, end, color, isArrow, isSpell) {
        const mesh = isArrow
            ? BABYLON.MeshBuilder.CreateBox("arrow", { width: 0.1, height: 0.1, depth: 2 }, this.scene)
            : BABYLON.MeshBuilder.CreateSphere("orb", { diameter: 0.8 }, this.scene);

        mesh.position = start.clone();

        const mat = new BABYLON.StandardMaterial("projMat", this.scene);
        mat.diffuseColor = color;
        mat.emissiveColor = color;
        mesh.material = mat;

        // Rotate arrow to face target
        mesh.lookAt(end);

        // Particle Trail?
        let particleSystem = null;
        if (isSpell) {
            particleSystem = new BABYLON.ParticleSystem("particles", 200, this.scene);
            particleSystem.emitter = mesh;
            particleSystem.particleTexture = new BABYLON.Texture("https://www.babylonjs-playground.com/textures/flare.png", this.scene);
            particleSystem.color1 = new BABYLON.Color4(color.r, color.g, color.b, 1.0);
            particleSystem.color2 = new BABYLON.Color4(color.r * 0.5, color.g * 0.5, color.b * 0.5, 1.0);
            particleSystem.colorDead = new BABYLON.Color4(0, 0, 0, 0.0);
            particleSystem.minSize = 0.1;
            particleSystem.maxSize = 0.5;
            particleSystem.minLifeTime = 0.2;
            particleSystem.maxLifeTime = 0.5;
            particleSystem.emitRate = 100;
            particleSystem.start();
        }

        // Animate
        const dist = BABYLON.Vector3.Distance(start, end);
        const frames = 30; // 0.5s speed

        BABYLON.Animation.CreateAndStartAnimation("projMove", mesh, "position", 60, frames, start, end, 0, null, () => {
            // Impact
            if (particleSystem) {
                particleSystem.stop();
                setTimeout(() => particleSystem.dispose(), 1000); // Allow trails to fade
            }
            mesh.dispose();
            sh.spawnExplosion(end, color);
        });
    };

    // HELPER: Explosion
    sh.spawnExplosion = function (pos, color) {
        const boom = BABYLON.MeshBuilder.CreateSphere("boom", { diameter: 2 }, this.scene);
        boom.position = pos;
        const mat = new BABYLON.StandardMaterial("boomMat", this.scene);
        mat.diffuseColor = color;
        mat.alpha = 0.8;
        boom.material = mat;

        // Expand and fade
        const frameRate = 30;
        const scaleAnim = new BABYLON.Animation("scale", "scaling", frameRate, BABYLON.Animation.ANIMATIONTYPE_VECTOR3, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        scaleAnim.setKeys([{ frame: 0, value: new BABYLON.Vector3(1, 1, 1) }, { frame: 15, value: new BABYLON.Vector3(3, 3, 3) }]);

        const fadeAnim = new BABYLON.Animation("fade", "visibility", frameRate, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        fadeAnim.setKeys([{ frame: 0, value: 0.8 }, { frame: 15, value: 0 }]);

        this.scene.beginDirectAnimation(boom, [scaleAnim, fadeAnim], 0, 15, false, 1.0, () => boom.dispose());
    };

    // HELPER: Smite
    sh.spawnSmiteEffect = function (pos) {
        const bolt = BABYLON.MeshBuilder.CreateCylinder("smite", { height: 20, diameter: 1 }, this.scene);
        bolt.position = pos.clone();
        bolt.position.y += 10;

        const mat = new BABYLON.StandardMaterial("smiteMat", this.scene);
        mat.diffuseColor = BABYLON.Color3.Yellow();
        mat.emissiveColor = BABYLON.Color3.Yellow();
        mat.alpha = 0.6;
        bolt.material = mat;

        // Flash
        const anim = new BABYLON.Animation("smiteAnim", "scaling.x", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        anim.setKeys([{ frame: 0, value: 0.1 }, { frame: 10, value: 5 }, { frame: 20, value: 0.1 }]);

        // Also scale Z
        const animZ = new BABYLON.Animation("smiteAnimZ", "scaling.z", 60, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        animZ.setKeys([{ frame: 0, value: 0.1 }, { frame: 10, value: 5 }, { frame: 20, value: 0.1 }]);

        this.scene.beginDirectAnimation(bolt, [anim, animZ], 0, 25, false, 1.0, () => bolt.dispose());
    };

    /**
     * Executes a timeline of animations on a prop hierarchy.
     * @param {Array} timeline - List of event objects { Time, Action, TargetId, Value, Duration }
     * @param {BABYLON.TransformNode} rootNode - The root node of the prop
     */
    sh.playTimeline = function (timeline, rootNode) {
        if (!timeline || timeline.length === 0) return;

        console.log(`[Animator] Playing Timeline for ${rootNode.name} (${timeline.length} total events)`);
        const fps = 60;

        // 1. Group events by Target -> Property
        // Key: "TargetId|Action" -> [Event, Event...]
        const tracks = {};

        timeline.forEach(event => {
            const targetId = sh.getProp(event, "TargetId");
            const action = sh.getProp(event, "Action");

            // Map Action to Babylon Property
            let property = "";
            let animType = BABYLON.Animation.ANIMATIONTYPE_FLOAT;

            if (action === "Scale") {
                property = "scaling";
                animType = BABYLON.Animation.ANIMATIONTYPE_VECTOR3;
            }
            else if (action === "Move") {
                property = "position";
                animType = BABYLON.Animation.ANIMATIONTYPE_VECTOR3;
            }
            else if (action === "Rotate") {
                property = "rotation";
                animType = BABYLON.Animation.ANIMATIONTYPE_VECTOR3;
            }
            else if (action === "Color") {
                // Defer property check until we find mesh, but use generic key for grouping
                property = "material.color";
                animType = BABYLON.Animation.ANIMATIONTYPE_COLOR3;
            }

            const key = targetId + "|" + property;
            if (!tracks[key]) tracks[key] = { targetId, action, property, animType, events: [] };
            tracks[key].events.push(event);
        });

        // 2. Process each track
        for (const key in tracks) {
            const track = tracks[key];
            const targetId = track.targetId;

            // Find Mesh
            let targetMesh = null;
            rootNode.getChildren(null, true).forEach(child => {
                if (child.name.endsWith("_" + targetId)) targetMesh = child;
            });
            if (!targetMesh && rootNode.name.endsWith(targetId)) targetMesh = rootNode;

            if (!targetMesh) {
                console.warn(`[Animator] Target '${targetId}' not found for track ${key}`);
                continue;
            }

            // Resolve Color Property (Standard vs PBR)
            if (track.action === "Color") {
                if (targetMesh.material) {
                    if (targetMesh.material.diffuseColor) track.property = "material.diffuseColor";
                    else if (targetMesh.material.albedoColor) track.property = "material.albedoColor";
                    else if (targetMesh.material.emissiveColor) track.property = "material.emissiveColor";
                }
            }

            // Sort events by time
            track.events.sort((a, b) => (sh.getProp(a, "Time") || 0) - (sh.getProp(b, "Time") || 0));

            // Create Animation
            const animName = "anim_" + key.replace("|", "_");
            const anim = new BABYLON.Animation(animName, track.property, fps, track.animType, BABYLON.Animation.ANIMATIONLOOPMODE_CYCLE);

            const keys = [];

            // Initial State (Frame 0)
            // We must have a Value at Frame 0, OR we assume current state.
            // If the first event starts > 0, we insert a key at 0 with current value.

            let currentVal = null;
            if (track.property === "scaling") currentVal = targetMesh.scaling.clone();
            else if (track.property === "position") currentVal = targetMesh.position.clone();
            else if (track.property === "rotation") currentVal = targetMesh.rotation.clone();
            else if (track.property.includes("Color")) {
                // Fetch generic color
                if (track.property.includes("diffuse")) currentVal = targetMesh.material.diffuseColor.clone();
                else if (track.property.includes("albedo")) currentVal = targetMesh.material.albedoColor.clone();
                else if (track.property.includes("emissive")) currentVal = targetMesh.material.emissiveColor.clone();
                else currentVal = BABYLON.Color3.White();
            }

            // Add Frame 0 key if needed?
            // Actually, let's just process events into keys.
            // Logic: Event (Time T, Duration D, Value V).
            // Means: From T to T+D, interpolate to V.
            // Start Value? Implied to be "Previous Value".

            // We verify if we need an explicit start key at T=0
            const firstTime = sh.getProp(track.events[0], "Time") || 0;
            if (firstTime > 0) {
                keys.push({ frame: 0, value: currentVal });
            }

            let lastFrame = 0;

            track.events.forEach(e => {
                const duration = sh.getProp(e, "Duration") || 0;
                const time = sh.getProp(e, "Time") || 0;
                const rawVal = sh.getProp(e, "Value");

                let val = null;
                if (track.animType === BABYLON.Animation.ANIMATIONTYPE_VECTOR3) {
                    val = new BABYLON.Vector3(rawVal[0], rawVal[1], rawVal[2]);
                    if (track.action === "Rotate") {
                        val = new BABYLON.Vector3(
                            BABYLON.Tools.ToRadians(rawVal[0]),
                            BABYLON.Tools.ToRadians(rawVal[1]),
                            BABYLON.Tools.ToRadians(rawVal[2])
                        );
                    }
                } else if (track.animType === BABYLON.Animation.ANIMATIONTYPE_COLOR3) {
                    val = new BABYLON.Color3(rawVal[0], rawVal[1], rawVal[2]);
                }

                const startFrame = time * fps;
                const endFrame = (time + duration) * fps;

                // If gap between lastFrame and startFrame? Hold value.
                // But Babylon handles gaps by interpolation if keys exist.
                // We want to "Hold" previous value until startFrame?
                // If I have key at 0, and next event starts at 30... 0->30 interpolates.
                // If I want 0->30 to be STATIC, I need a key at 30 (Start) with SAME value as 0.
                // But we don't know the value at 30 unless we calculated it.

                // SIMPLIFIED APPROACH:
                // Just use the End target.
                // Key at Time+Duration = Value.
                // If there is ANY previous key, Babylon interpolates.
                // This matches the "Tween" definition of "Go to X".

                // Special case: Instant Set (Duration 0)
                // We set a key at Frame T with Value.

                keys.push({ frame: endFrame, value: val });

                if (endFrame > lastFrame) lastFrame = endFrame;
            });

            // Check loop closure?
            // If we want a perfect loop, the last key must match first key?
            // The JSON data should define this (e.g. last event returns to 0).

            anim.setKeys(keys);
            targetMesh.animations.push(anim);
            this.scene.beginAnimation(targetMesh, 0, lastFrame, true);
        }
    };
    // HELPER: Flash Target
    sh.flashTarget = function (targetId, colorHex, duration) {
        const targetNode = this.scene.getTransformNodeByName("voxel_" + targetId) || this.scene.getMeshByName("voxel_" + targetId) || this.scene.getTransformNodeByName(targetId);
        if (!targetNode) return;

        const color = colorHex ? BABYLON.Color3.FromHexString(colorHex) : BABYLON.Color3.White();

        targetNode.getChildren(null, true).forEach(mesh => {
            if (mesh.material && mesh.material.diffuseColor) {
                // Store original color if not already stored? 
                // Simple version: just flash to color and back to original (assumed white/texture or current)
                // Better: clone current, set to flash, restore.

                // For Voxel models, we might need emissive to make it "bright"
                const oldEmissive = mesh.material.emissiveColor.clone();
                mesh.material.emissiveColor = color;

                setTimeout(() => {
                    if (mesh.material) mesh.material.emissiveColor = oldEmissive;
                }, duration || 200);
            }
        });
    };
})();
