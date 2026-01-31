/**
 * StoneHammer v10.0: Core Module
 * Main entry point, engine initialization, and render loop.
 */
window.stoneHammer = {
    engine: null,
    scene: null,
    canvas: null,
    player: null,
    inputMap: {},
    playerSpeed: 0.25,
    playerRotationSpeed: 0.05,
    walkTime: 0,
    buildingTriggers: [],
    currentBuilding: null,
    dotNetHelper: null,

    setDotNetHelper: function (helper) {
        this.dotNetHelper = helper;
        this.log("JS Interop Helper Registered", "lime");
    },

    init: function (canvasId) {
        try {
            this.log("Engine Init v10.0 (Modular Refactor)", "cyan");
            this.canvas = document.getElementById(canvasId);
            this.engine = new BABYLON.Engine(this.canvas, true);
            this.scene = new BABYLON.Scene(this.engine);
            this.scene.samples = 4;

            // Global Esc Register
            window.addEventListener("keydown", (e) => {
                if (e.key === "Escape" && this.currentBuilding) {
                    this.exitBuilding();
                }
            });

            this.scene.clearColor = new BABYLON.Color4(0.5, 0.7, 1.0, 1);

            // Hard Skybox
            var skybox = BABYLON.MeshBuilder.CreateBox("skyBox", { size: 5000.0 }, this.scene);
            var skyboxMaterial = new BABYLON.StandardMaterial("skyBox", this.scene);
            skyboxMaterial.backFaceCulling = false;
            skyboxMaterial.disableLighting = true;
            skyboxMaterial.diffuseColor = new BABYLON.Color3(0.5, 0.7, 1.0);
            skyboxMaterial.emissiveColor = new BABYLON.Color3(0.5, 0.7, 1.0);
            skybox.material = skyboxMaterial;
            skybox.infiniteDistance = true;
            skybox.isPickable = false;

            // Stable Camera
            this.camera = new BABYLON.ArcRotateCamera("camera1", -Math.PI / 2, Math.PI / 3, 20, BABYLON.Vector3.Zero(), this.scene);
            this.camera.attachControl(this.canvas, true);
            this.camera.upperRadiusLimit = 1000;
            this.camera.lowerRadiusLimit = 2;
            this.camera.minZ = 0.5;
            this.camera.maxZ = 10000;

            // Stable Lighting
            var hemi = new BABYLON.HemisphericLight("hemi", new BABYLON.Vector3(0, 1, 0), this.scene);
            hemi.intensity = 1.5;
            hemi.groundColor = new BABYLON.Color3(0.1, 0.1, 0.2);

            var dir = new BABYLON.DirectionalLight("dir", new BABYLON.Vector3(-1, -2, -1), this.scene);
            dir.position = new BABYLON.Vector3(50, 100, 50);
            dir.intensity = 1.2;

            // v10.1: Ground is now managed as a JSON asset ("town_floor.json")
            // No hardcoded ground reference here anymore.

            this.setupClickInteraction();
            this.setupInput();

            this.engine.runRenderLoop(() => {
                this.scene.render();
                this.updateAnimations();
            });
            window.addEventListener("resize", () => { this.engine.resize(); });

            this.log("StoneHammer v11.5 Online (Modularized)", "lime");
        } catch (err) {
            this.log("CRITICAL ERR: " + err.message, "red");
        }
    },

    setupInput: function () {
        this.inputMap = {};
        window.addEventListener("keydown", (evt) => { this.inputMap[evt.key.toLowerCase()] = true; });
        window.addEventListener("keyup", (evt) => { this.inputMap[evt.key.toLowerCase()] = false; });
        this.log("Input management initialized", "cyan");
    }
};
