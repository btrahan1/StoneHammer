(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    // v16.0: Character Visual Logic
    sh.character = {

        // Called when class changes to update specific visuals (Armor colors, weapon attachment points, etc)
        setClassVisuals: function (className) {
            console.log("[Character] Setting visuals for class: " + className);

            // Find the player
            const player = sh.scene.getNodes().find(n => n.name === "voxel_Player" || n.name === "voxel_Master Mason");
            if (!player) return;

            // Example: Change Tunic Color based on Class
            // We need to access the material or specific voxel parts.
            // Since we use Voxel parts, we would look for "Player_torso" and change its color.

            const colorMap = {
                "Fighter": new BABYLON.Color3(0.5, 0.1, 0.1), // Red
                "Rogue": new BABYLON.Color3(0.1, 0.1, 0.1), // Black
                "Healer": new BABYLON.Color3(0.8, 0.8, 0.8), // White
                "Mage": new BABYLON.Color3(0.2, 0.2, 0.8)  // Blue
            };

            if (colorMap[className]) {
                const torso = player.getChildren((n) => n.name.includes("torso"), false)[0];
                if (torso && torso.material) {
                    torso.material.diffuseColor = colorMap[className];
                }
            }
        },

        playSkillEffect: function (skillName, origin) {
            console.log("[Character] Playing effect: " + skillName);
            // Particle system placeholders
            if (skillName === "Fireball") {
                // Spawn fireball mesh/particle
                const sphere = BABYLON.MeshBuilder.CreateSphere("fireball", { diameter: 0.5 }, sh.scene);
                sphere.material = new BABYLON.StandardMaterial("fire", sh.scene);
                sphere.material.emissiveColor = BABYLON.Color3.Red();
                sphere.position = origin.clone().add(new BABYLON.Vector3(0, 1.5, 0));

                // Shoot forward
                // ...
                setTimeout(() => sphere.dispose(), 1000);
            }
        }
    };

    console.log("StoneHammer: Character Module Loaded");
})();
