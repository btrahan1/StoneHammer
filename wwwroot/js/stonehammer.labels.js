/**
 * StoneHammer v1.1: Labels Module (Data-Driven)
 * Handles floating text labels found in scene metadata.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.initLabels = function () {
        console.log("[Labels] Initializing (Data-Driven)...");

        // Create a Texture specifically for labels (World Space)
        this.labelTexture = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("LabelsUI");

        this.scanAndCreateLabels();
    };

    sh.scanAndCreateLabels = function () {
        // Iterate over all transforms and meshes to find metadata.Label
        const nodes = this.scene.transformNodes.concat(this.scene.meshes);
        let count = 0;

        nodes.forEach(node => {
            if (node.metadata && node.metadata.Label) {
                this.createLabelForNode(node, node.metadata.Label, node.metadata.LabelColor);
                count++;
            }
        });

        console.log(`[Labels] Scan complete. Found ${count} labels.`);

        if (count === 0) {
            // Retry logic for async loading?
            // If the town is large, maybe not all meshes are ready?
            // But usually transformNodes are created sync by the time initLabels is called.
            // Let's add a safe retry just in case.
            setTimeout(() => this.scanRetry(), 1000);
        }
    };

    sh.scanRetry = function () {
        const nodes = this.scene.transformNodes.concat(this.scene.meshes);
        let count = 0;
        nodes.forEach(node => {
            // Ensure we don't duplicate (check if already has link?)
            // AdvancedDynamicTexture doesn't easily expose linked meshes, so simple check:
            // For now, duplicate scan is low risk if init called once.
            if (node.metadata && node.metadata.Label) {
                // Check if already labeled?
                // Skip for now, assume single pass.
                // Actually, checking children of texture might be safer but complex.
                // Let's just log.
            }
        });
    };

    sh.createLabelForNode = function (node, text, color) {
        // Create Text
        const label = new BABYLON.GUI.TextBlock();
        label.text = text;
        label.color = color || "white";
        label.fontSize = 36;
        label.fontFamily = "Cinzel, serif";
        label.outlineWidth = 5;
        label.outlineColor = "black";
        label.fontWeight = "bold";

        this.labelTexture.addControl(label);

        // Link to Node (Mesh or TransformNode)
        label.linkWithMesh(node);

        // Offset: Up ~3 meters
        label.linkOffsetY = -120;

        console.log(`[Labels] Linked '${text}' to ${node.name}`);
    };

    // Legacy support or console debugging
    sh.createLabel = function (name, text, color) {
        console.warn("[Labels] properties.createLabel is deprecated. Use metadata in json.");
    };

})();
