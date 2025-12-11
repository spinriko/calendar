const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const distDir = path.join(__dirname, '..', 'wwwroot', 'dist');
const manifestPath = path.join(distDir, 'asset-manifest.json');

function isHashed(filename) {
    return /\.[a-f0-9]{8}\.\w+$/.test(filename);
}

function computeHash(content) {
    return crypto.createHash('sha256').update(content).digest('hex').slice(0, 8);
}

function updateSourceMapReference(jsPath, newMapName) {
    let content = fs.readFileSync(jsPath, 'utf8');
    // replace //# sourceMappingURL=... or /*# sourceMappingURL=... */
    content = content.replace(/(\/\/[#@]\s?sourceMappingURL=)([^\s'"\n\r]+)/, `$1${newMapName}`);
    content = content.replace(/(\/\*[#@]\s?sourceMappingURL=)([^*]+)(\*\/)/, `$1${newMapName}$3`);
    fs.writeFileSync(jsPath, content, 'utf8');
}

function generate() {
    if (!fs.existsSync(distDir)) {
        console.error('dist directory not found:', distDir);
        process.exit(1);
    }

    const entries = fs.readdirSync(distDir);

    // Work on JS and CSS files that are not already hashed.
    const candidates = entries.filter(f => (f.endsWith('.js') || f.endsWith('.css')) && !isHashed(f));

    // We'll produce a mapping from logical name (e.g. site.js) => /dist/hashed-file
    const manifest = {};

    // First, preserve already-hashed files in the manifest (so repeated runs are idempotent)
    entries.forEach(f => {
        if (isHashed(f) && (f.endsWith('.js') || f.endsWith('.css'))) {
            // derive base logical name: remove .{hash} from name
            const m = f.match(/^(.*)\.([a-f0-9]{8})\.(js|css)$/);
            if (m) {
                const logical = `${m[1]}.${m[3]}`;
                manifest[logical] = '/' + path.posix.join('dist', f);
            }
        }
    });

    // Process un-hashed files: compute hash, rename, handle source maps
    candidates.forEach(f => {
        const full = path.join(distDir, f);
        const content = fs.readFileSync(full);
        const hash = computeHash(content);
        const ext = path.extname(f); // .js or .css
        const base = path.basename(f, ext);
        const newName = `${base}.${hash}${ext}`;
        const newFull = path.join(distDir, newName);

        // Rename the main file
        fs.renameSync(full, newFull);

        // If there's a source map like `site.js.map`, rename it to `site.{hash}.js.map`
        const mapName = f + '.map';
        const mapFull = path.join(distDir, mapName);
        if (fs.existsSync(mapFull)) {
            const newMapName = `${base}.${hash}${ext}.map`;
            const newMapFull = path.join(distDir, newMapName);
            fs.renameSync(mapFull, newMapFull);
            // Update sourceMappingURL inside the renamed JS file to point to the new map filename
            if (ext === '.js') {
                updateSourceMapReference(newFull, newMapName);
            }
        }

        // Add to manifest mapping logical name -> hashed path
        const logical = `${base}${ext}`;
        manifest[logical] = '/' + path.posix.join('dist', newName);
    });

    fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
    console.log('Wrote manifest:', manifestPath);
}

generate();
