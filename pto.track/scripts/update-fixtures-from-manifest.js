const fs = require('fs').promises;
const path = require('path');

async function main() {
    const repoRoot = path.join(__dirname, '..');
    const manifestPath = path.join(repoRoot, 'wwwroot', 'dist', 'asset-manifest.json');
    let manifest;
    try {
        const raw = await fs.readFile(manifestPath, 'utf8');
        manifest = JSON.parse(raw);
    } catch (err) {
        console.error('Could not read asset manifest at', manifestPath, err.message);
        process.exitCode = 2;
        return;
    }

    const responsesDir = path.join(repoRoot, '..', '..', 'responses');
    // Normalize to allow running from project root or repo root
    // Also check relative to repo root (pto.track/responses)
    const candidates = [
        path.join(repoRoot, 'responses'),
        path.join(repoRoot, '..', 'responses'),
        responsesDir
    ];

    let found = false;
    for (const dir of candidates) {
        try {
            const stat = await fs.stat(dir);
            if (!stat.isDirectory()) continue;
        } catch (_) { continue; }

        found = true;
        console.log('Updating fixtures in', dir);
        await updateDir(dir, manifest);
    }

    if (!found) {
        console.log('No responses/ directory found in known locations. Nothing to update.');
    }
}

async function updateDir(dir, manifest) {
    const entries = await fs.readdir(dir, { withFileTypes: true });
    for (const ent of entries) {
        const p = path.join(dir, ent.name);
        if (ent.isDirectory()) {
            await updateDir(p, manifest);
            continue;
        }

        if (!p.endsWith('.json') && !p.endsWith('.html') && !p.endsWith('.js')) continue;

        let content = await fs.readFile(p, 'utf8');
        let replaced = false;

        // Replace logical keys like /dist/absences-scheduler.js with hashed mapping
        for (const key of Object.keys(manifest)) {
            const logical = '/dist/' + key;
            const mapped = manifest[key];
            if (!mapped) continue;
            if (content.indexOf(logical) !== -1) {
                content = content.split(logical).join(mapped);
                replaced = true;
            }
        }

        if (replaced) {
            await fs.writeFile(p, content, 'utf8');
            console.log('Patched', p);
        }
    }
}

main().catch(err => {
    console.error(err);
    process.exitCode = 1;
});
