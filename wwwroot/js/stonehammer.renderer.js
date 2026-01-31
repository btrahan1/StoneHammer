/**
 * StoneHammer v10.0: Renderer Module
 * Procedural material system and mesh configuration.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.createMaterial = function (id, config) {
        const type = (this.getProp(config, "Material") || "Plastic").toLowerCase();
        const colorHex = this.getProp(config, "ColorHex") || "#888888";
        const color = BABYLON.Color3.FromHexString(colorHex);

        // v9.20/v10.0: Wood Material (Procedural Planks)
        if (type.includes("wood")) {
            const woodMat = new BABYLON.StandardMaterial("wood_" + id, this.scene);
            const woodTex = new BABYLON.BrickProceduralTexture("woodTex_" + id, 512, this.scene);
            woodTex.numberOfBricksHeight = 40; // Thin planks
            woodTex.numberOfBricksWidth = 4;
            woodTex.brickColor = color;
            woodTex.jointColor = new BABYLON.Color3(0.1, 0.05, 0); // Dark wood joint
            woodMat.diffuseTexture = woodTex;
            return woodMat;
        }

        // v10.2: Sand Material (Softened)
        if (type.includes("sand")) {
            const sandMat = new BABYLON.StandardMaterial("sand_" + id, this.scene);
            const noise = new BABYLON.NoiseProceduralTexture("sandNoise_" + id, 512, this.scene);
            noise.octaves = 4;
            noise.persistence = 0.5;
            noise.brightness = 0.6; // Brighter highlights
            noise.animationSpeedFactor = 0;
            sandMat.diffuseTexture = noise;
            sandMat.diffuseColor = color;
            return sandMat;
        }

        // v9.16: Official Procedural Brick (For Floors/Plazas)
        if (type.includes("brick") || type.includes("grid")) {
            const brickMat = new BABYLON.StandardMaterial("brick_" + id, this.scene);
            const brickTex = new BABYLON.BrickProceduralTexture("brickTex_" + id, 512, this.scene);
            brickTex.numberOfBricksHeight = 20;
            brickTex.numberOfBricksWidth = 20;
            brickTex.brickColor = new BABYLON.Color3(0.3, 0.3, 0.35); // Dark Stone
            brickTex.jointColor = new BABYLON.Color3(0.1, 0.1, 0.1); // Black Grout
            brickMat.diffuseTexture = brickTex;
            return brickMat;
        }

        // v10.2: Non-Animated Stone (Softened)
        if (type.includes("stone")) {
            const stoneMat = new BABYLON.StandardMaterial("stone_" + id, this.scene);
            const noise = new BABYLON.NoiseProceduralTexture("noise_" + id, 512, this.scene);
            noise.octaves = 4;
            noise.persistence = 0.5;
            noise.animationSpeedFactor = 0; // Frozen solid
            noise.uScale = 5;
            noise.vScale = 5;
            stoneMat.diffuseTexture = noise;
            stoneMat.diffuseColor = color;
            return stoneMat;
        }

        const mat = new BABYLON.PBRMaterial("mat_" + id, this.scene);
        mat.albedoColor = color;
        if (this.scene.environmentTexture) mat.reflectionTexture = this.scene.environmentTexture;

        if (type.includes("metal")) {
            mat.metallic = 1.0;
            mat.roughness = 0.1;
        } else if (type.includes("glass")) {
            mat.metallic = 0.1;
            mat.roughness = 0.05;
            mat.alpha = 0.3;
            mat.transparencyMode = BABYLON.PBRMaterial.PBRMATERIAL_ALPHABLEND;
        } else if (type.includes("glow")) {
            mat.emissiveColor = color;
            mat.emissiveIntensity = 2.0;
        } else {
            mat.metallic = 0.0;
            mat.roughness = 0.7;
        }
        return mat;
    };
})();
