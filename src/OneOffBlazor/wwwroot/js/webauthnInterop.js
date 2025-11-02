// -----------------------------------------------------------
//  WebAuthn Interop for Blazor (safe for Fido2NetLib)
// -----------------------------------------------------------

// ---------- Base64URL helpers ----------
function toBase64Url(bytes) {
    return btoa(String.fromCharCode.apply(null, new Uint8Array(bytes)))
        .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function fromBase64Url(base64url) {
    if (!base64url) return new ArrayBuffer(0);
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

    // Convert challenge and user.id to ArrayBuffer
    publicKey.challenge = fromBase64Url(publicKey.challenge);
    if (publicKey.user && publicKey.user.id)
        publicKey.user.id = fromBase64Url(publicKey.user.id);

    // Normalize attestation
    publicKey.attestation = (publicKey.attestation || "none").toLowerCase();

    // Normalize COSE algorithm identifiers
    if (publicKey.pubKeyCredParams) {
        publicKey.pubKeyCredParams = publicKey.pubKeyCredParams.map(p => ({
            type: "public-key", // browser expects lowercase
            alg: (p.alg === "ES256" || p.alg === -7) ? -7 :
                (p.alg === "RS256" || p.alg === -257) ? -257 :
                    (p.alg === "EdDSA" || p.alg === -8) ? -8 :
                        p.alg
        }));
    }

    // Normalize authenticator selection fields
    if (publicKey.authenticatorSelection) {
        const sel = publicKey.authenticatorSelection;
        if (sel.authenticatorAttachment)
            sel.authenticatorAttachment = sel.authenticatorAttachment.toLowerCase();
        if (sel.residentKey)
            sel.residentKey = sel.residentKey.toLowerCase();
        if (sel.userVerification)
            sel.userVerification = sel.userVerification.toLowerCase();
    }

    // Normalize exclude credentials
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

    // Create the credential via WebAuthn
    const cred = await navigator.credentials.create({publicKey});

    // Normalize output for Fido2NetLib (case-sensitive)
    return {
        id: cred.id,
        rawId: toBase64Url(cred.rawId),
        type: "PublicKey",
        response: {
            attestationObject: toBase64Url(cred.response.attestationObject),
            clientDataJSON: toBase64Url(cred.response.clientDataJSON),
            transports: cred.response.getTransports
                ? cred.response.getTransports()
                : [], // Some browsers expose this method
        },
        clientExtensionResults: cred.getClientExtensionResults
            ? cred.getClientExtensionResults()
            : {}
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

    // Get assertion via WebAuthn
    const assertion = await navigator.credentials.get({publicKey});

    // Normalize for Fido2NetLib (server expects type = "PublicKey")
    return {
        id: assertion.id,
        rawId: toBase64Url(assertion.rawId),
        type: "PublicKey",
        response: {
            authenticatorData: toBase64Url(assertion.response.authenticatorData),
            clientDataJSON: toBase64Url(assertion.response.clientDataJSON),
            signature: toBase64Url(assertion.response.signature),
            userHandle: assertion.response.userHandle
                ? toBase64Url(assertion.response.userHandle)
                : null
        },
        clientExtensionResults: assertion.getClientExtensionResults
            ? assertion.getClientExtensionResults()
            : {}
    };

};
