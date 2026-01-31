/**
 * StoneHammer v14.0: Desert Module
 * Refactored port of MadMaxBlazor wasteland generation.
 * Handles procedural terrain, atmosphere, and prop scattering.
 */
window.stoneHammer.desert = {
    scene: null,
    ground: null,
    desertMeshes: [],

    // Config
    width: 2000,
    height: 2000,
    subdivisions: 200, // Reduced from 300 for optimization

    init: function (scene) {
        this.scene = scene;
        this.desertMeshes = [];

        stoneHammer.log("Initializing Desert Environment...", "orange");

        // 1. Atmosphere (Mad Max Style)
        scene.clearColor = new BABYLON.Color3(0.8, 0.5, 0.2);
        scene.fogMode = BABYLON.Scene.FOGMODE_EXP2;
        scene.fogDensity = 0.005;
        scene.fogColor = new BABYLON.Color3(0.7, 0.5, 0.3);

        // 2. Adjust Lighting
        // We find existing lights and tweak them, or add strictly desert lights
        // For StoneHammer, we enter this mode exclusively, so we can tweak globals.
        var hemi = scene.getLightByName("hemi");
        if (hemi) {
            hemi.intensity = 0.6;
            hemi.diffuse = new BABYLON.Color3(0.4, 0.2, 0.1);
            hemi.groundColor = new BABYLON.Color3(0.1, 0.1, 0.0);
        }

        // 3. Terrain
        this.createTerrain();

        // 4. Props
        this.createRuins(8);
        this.createVegetation(100);
        this.createScrap(15);
    },

    cleanup: function () {
        stoneHammer.log("Leaving the Desert...", "cyan");
        // Dispose all procedural meshes
        this.desertMeshes.forEach(m => m.dispose());
        this.desertMeshes = [];
        this.ground = null;

        // Restore Atmosphere (Town Defaults from stonehammer.core.js)
        if (this.scene) {
            this.scene.clearColor = new BABYLON.Color4(0.5, 0.7, 1.0, 1);
            this.scene.fogDensity = 0; // Disable fog or reset to default

            var hemi = this.scene.getLightByName("hemi");
            if (hemi) {
                hemi.intensity = 1.5;
                hemi.diffuse = new BABYLON.Color3(1, 1, 1);
                hemi.groundColor = new BABYLON.Color3(0.1, 0.1, 0.2);
            }
        }
    },

    calculateHeight: function (x, z) {
        // Sine waves for rolling dunes
        // Layer 1: Large Rolling Hills
        var y = Math.sin(x * 0.02) * 5 + Math.cos(z * 0.02) * 5;
        // Layer 2: Smaller ripples
        y += Math.sin(x * 0.1) * 1 + Math.cos(z * 0.1) * 1;
        return y;
    },

    getHeightAt: function (x, z) {
        // O(1) math lookup is preferred over raycasting for generation
        return this.calculateHeight(x, z);
    },

    createTerrain: function () {
        var ground = BABYLON.MeshBuilder.CreateGround("desert_ground", {
            width: this.width,
            height: this.height,
            subdivisions: this.subdivisions,
            updatable: true
        }, this.scene);

        // Material
        var mat = new BABYLON.StandardMaterial("sandMat", this.scene);
        mat.diffuseTexture = new BABYLON.Texture("https://www.babylonjs-playground.com/textures/sand.jpg", this.scene);
        mat.diffuseTexture.uScale = 50;
        mat.diffuseTexture.vScale = 50;
        mat.specularColor = new BABYLON.Color3(0, 0, 0); // Sand isn't shiny
        mat.roughness = 1.0;
        ground.material = mat;

        // Peak Deformation
        var positions = ground.getVerticesData(BABYLON.VertexBuffer.PositionKind);
        for (var i = 0; i < positions.length; i += 3) {
            var x = positions[i];
            var z = positions[i + 2];
            positions[i + 1] = this.calculateHeight(x, z);
        }
        ground.updateVerticesData(BABYLON.VertexBuffer.PositionKind, positions);

        // Normals
        var indices = ground.getIndices();
        var normals = ground.getVerticesData(BABYLON.VertexBuffer.NormalKind);
        BABYLON.VertexData.ComputeNormals(positions, indices, normals);
        ground.updateVerticesData(BABYLON.VertexBuffer.NormalKind, normals);

        this.ground = ground;
        this.desertMeshes.push(ground);
    },

    createRuins: function (count) {
        var concreteMat = new BABYLON.StandardMaterial("concMat", this.scene);
        concreteMat.diffuseColor = new BABYLON.Color3(0.4, 0.4, 0.45);

        for (var i = 0; i < count; i++) {
            var x = (Math.random() * 800) - 400;
            var z = (Math.random() * 800) - 400;
            // Avoid spawning on top of spawn point (0,0)
            if (Math.abs(x) < 50 && Math.abs(z) < 50) continue;

            var y = this.getHeightAt(x, z);

            // Simple Obelisk Ruin
            var base = BABYLON.MeshBuilder.CreateBox("ruin_base_" + i, { width: 10, height: 20, depth: 10 }, this.scene);
            base.position = new BABYLON.Vector3(x, y + 5, z);
            base.rotation.y = Math.random() * Math.PI;
            base.rotation.z = (Math.random() - 0.5) * 0.5; // Slight tilt
            base.material = concreteMat;

            this.desertMeshes.push(base);
        }
    },

    createVegetation: function (count) {
        var mat = new BABYLON.StandardMaterial("cactusMat", this.scene);
        mat.diffuseColor = new BABYLON.Color3(0.3, 0.5, 0.2);

        for (var i = 0; i < count; i++) {
            var x = (Math.random() * 1000) - 500;
            var z = (Math.random() * 1000) - 500;
            if (Math.abs(x) < 20 && Math.abs(z) < 20) continue;

            var y = this.getHeightAt(x, z);
            var h = 3 + Math.random() * 3;

            var cactus = BABYLON.MeshBuilder.CreateCylinder("cactus_" + i, { diameter: 0.8, height: h }, this.scene);
            cactus.position = new BABYLON.Vector3(x, y + (h / 2), z);
            cactus.material = mat;

            this.desertMeshes.push(cactus);
        }
    },

    createScrap: function (count) {
        var mat = new BABYLON.StandardMaterial("scrapMat", this.scene);
        mat.diffuseColor = new BABYLON.Color3(0.4, 0.2, 0.1); // Rust

        for (var i = 0; i < count; i++) {
            var x = (Math.random() * 600) - 300;
            var z = (Math.random() * 600) - 300;
            var y = this.getHeightAt(x, z);

            var scrap = BABYLON.MeshBuilder.CreateBox("scrap_" + i, { size: 1.5 }, this.scene);
            scrap.position = new BABYLON.Vector3(x, y + 0.5, z);
            scrap.rotation = new BABYLON.Vector3(Math.random(), Math.random(), Math.random());
            scrap.material = mat;

            this.desertMeshes.push(scrap);
        }
    }
};
