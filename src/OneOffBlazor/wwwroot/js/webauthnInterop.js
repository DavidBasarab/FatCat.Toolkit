// ---------- Base64URL helpers ----------
function toBase64Url(bytes) {
    return btoa(String.fromCharCode.apply(null, new Uint8Array(bytes)))
        .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function fromBase64Url(base64url) {
    const pad = 4 - (base64url.length % 4);
    const base64 = (base64url + "====".substring(0, pad))
        .replace(/-/g, "+").replace(/_/g, "/");
    const str = atob(base64);
    const bytes = new Uint8Array(str.length);
    for (let i = 0; i < str.length; i++) bytes[i] = str.charCodeAt(i);
    return bytes.buffer;
}

// ---------- Normalization ----------
function fixOptions(publicKey) {
    if (!publicKey) throw new Error("Missing publicKey options");

    publicKey.challenge = fromBase64Url(publicKey.challenge);
    if (publicKey.user && publicKey.user.id)
        publicKey.user.id = fromBase64Url(publicKey.user.id);

    // Lowercase + numeric normalization
    publicKey.attestation = (publicKey.attestation || "none").toLowerCase();
    if (publicKey.pubKeyCredParams) {
        publicKey.pubKeyCredParams = publicKey.pubKeyCredParams.map(p => ({
            type: "public-key",
            alg: (p.alg === "ES256" || p.alg === -7) ? -7 :
                (p.alg === "RS256" || p.alg === -257) ? -257 : p.alg
        }));
    }

    if (publicKey.authenticatorSelection) {
        const sel = publicKey.authenticatorSelection;
        sel.authenticatorAttachment = sel.authenticatorAttachment?.toLowerCase();
        sel.residentKey = sel.residentKey?.toLowerCase();
        sel.userVerification = sel.userVerification?.toLowerCase();
    }

    if (publicKey.excludeCredentials) {
        publicKey.excludeCredentials = publicKey.excludeCredentials.map(c => ({
            ...c,
            id: fromBase64Url(c.id),
            type: "public-key"
        }));
    }

    return publicKey;
}

// ---------- Registration ----------
window.createCredential = async function (optionsJson) {
    const opts = JSON.parse(optionsJson);
    const publicKey = fixOptions(opts.publicKey);
    const cred = await navigator.credentials.create({ publicKey });

    return {
        id: cred.id,
        rawId: toBase64Url(cred.rawId),
        type: cred.type,
        response: {
            attestationObject: toBase64Url(cred.response.attestationObject),
            clientDataJSON: toBase64Url(cred.response.clientDataJSON)
        }
    };
};

// ---------- Login / Assertion ----------
window.getAssertion = async function (optionsJson) {
    const opts = JSON.parse(optionsJson);
    const publicKey = opts.publicKey;
    publicKey.challenge = fromBase64Url(publicKey.challenge);
    if (publicKey.allowCredentials) {
        publicKey.allowCredentials = publicKey.allowCredentials.map(c => ({
            ...c,
            id: fromBase64Url(c.id),
            type: "public-key"
        }));
    }

    const assertion = await navigator.credentials.get({ publicKey });
    return {
        id: assertion.id,
        rawId: toBase64Url(assertion.rawId),
        type: assertion.type,
        response: {
            authenticatorData: toBase64Url(assertion.response.authenticatorData),
            clientDataJSON: toBase64Url(assertion.response.clientDataJSON),
            signature: toBase64Url(assertion.response.signature),
            userHandle: assertion.response.userHandle ?
                toBase64Url(assertion.response.userHandle) : null
        }
    };
};
