const fs = require('fs');
const path = require('path');

function fail(msg, code = 1) {
    console.error(msg);
    process.exit(code);
}

const projectDir = process.argv[2] || path.join(__dirname, '..');
const distDir = path.join(projectDir, 'wwwroot', 'dist');
const manifestPath = path.join(distDir, 'asset-manifest.json');

if (!fs.existsSync(distDir)) {
    fail(`dist directory not found at ${distDir}`);
}

if (!fs.existsSync(manifestPath)) {
    fail(`asset-manifest.json not found at ${manifestPath}`);
}

let manifest;
try {
    manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
} catch (e) {
    fail(`Failed to parse manifest: ${e.message}`);
}

const missing = [];
Object.keys(manifest).forEach(key => {
    const mapped = manifest[key];
    // Expected URL path like /dist/file or /dist/sub/file
    let filePath;
    if (mapped.startsWith('/dist/') || mapped.startsWith('dist/')) {
        const rel = mapped.replace(/^\/?dist\//, '');
        filePath = path.join(projectDir, 'wwwroot', 'dist', rel.split('/').join(path.sep));
    } else {
        // fallback: map URL path directly under projectDir
        const rel = mapped.replace(/^\//, '');
        filePath = path.join(projectDir, rel.split('/').join(path.sep));
    }
    if (!fs.existsSync(filePath)) {
        missing.push({ key, mapped, filePath });
    }
});

if (missing.length > 0) {
    console.error('Manifest validation failed — missing files:');
    missing.forEach(m => console.error(`- ${m.key} -> ${m.mapped} (expected ${m.filePath})`));
    process.exit(2);
}

if (!manifest['site.js']) {
    console.warn('Warning: manifest does not contain `site.js` entry');
}

console.log('Manifest validation succeeded.');
process.exit(0);
