/**
 * StoneHammer v10.0: Utils Module
 * Utility functions for property access, logging, and vector parsing.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.getProp = function (obj, key) {
        if (!obj) return null;
        return obj[key] ?? obj[key.charAt(0).toLowerCase() + key.slice(1)] ?? obj[key.toUpperCase()] ?? null;
    };

    sh.log = function (msg, color = "white") {
        const el = document.getElementById("debug-log");
        if (el) {
            const div = document.createElement("div");
            div.style.color = color;
            div.innerText = "[" + new Date().toLocaleTimeString() + "] " + msg;
            el.prepend(div);
        }
        console.log("StoneHammer: " + msg);
    };

    sh.parseVec3 = function (data, defaultVal = { x: 0, y: 0, z: 0 }) {
        let def = defaultVal;
        if (typeof defaultVal === 'number') def = { x: defaultVal, y: defaultVal, z: defaultVal };

        if (!data) return new BABYLON.Vector3(def.x, def.y, def.z);
        if (Array.isArray(data)) return new BABYLON.Vector3(data[0] ?? def.x, data[1] ?? def.y, data[2] ?? def.z);
        if (typeof data === 'object') return new BABYLON.Vector3(data.x ?? data.X ?? def.x, data.y ?? data.Y ?? def.y, data.z ?? data.Z ?? def.z);
        return new BABYLON.Vector3(def.x, def.y, def.z);
    };
})();
