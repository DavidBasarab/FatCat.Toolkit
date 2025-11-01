function base64UrlToBase64(base64url) {
    // Convert from base64url (RFC 4648 §5) to base64
    return base64url.replace(/-/g, '+').replace(/_/g, '/')
        .padEnd(base64url.length + (4 - base64url.length % 4) % 4, '=');
}

window.webauthnInterop = {
    
    createCredential: async function (optionsJson) {
        const options = JSON.parse(optionsJson);

        options.challenge = Uint8Array.from(atob(base64UrlToBase64(options.challenge)), c => c.charCodeAt(0)).buffer;
        options.user.id = Uint8Array.from(atob(base64UrlToBase64(options.user.id)), c => c.charCodeAt(0)).buffer;

        const cred = await navigator.credentials.create({ publicKey: options });

        return JSON.stringify({
            id: cred.id,
            rawId: btoa(String.fromCharCode(...new Uint8Array(cred.rawId))),
            type: cred.type,
            response: {
                attestationObject: btoa(String.fromCharCode(...new Uint8Array(cred.response.attestationObject))),
                clientDataJSON: btoa(String.fromCharCode(...new Uint8Array(cred.response.clientDataJSON)))
            }
        });
    },

    getAssertion: async function (optionsJson) {
        const options = JSON.parse(optionsJson);

        options.challenge = Uint8Array.from(atob(base64UrlToBase64(options.challenge)), c => c.charCodeAt(0)).buffer;
        if (options.allowCredentials)
            options.allowCredentials.forEach(c => {
                c.id = Uint8Array.from(atob(base64UrlToBase64(c.id)), d => d.charCodeAt(0)).buffer;
            });

        const assertion = await navigator.credentials.get({ publicKey: options });

        return JSON.stringify({
            id: assertion.id,
            rawId: btoa(String.fromCharCode(...new Uint8Array(assertion.rawId))),
            type: assertion.type,
            response: {
                authenticatorData: btoa(String.fromCharCode(...new Uint8Array(assertion.response.authenticatorData))),
                clientDataJSON: btoa(String.fromCharCode(...new Uint8Array(assertion.response.clientDataJSON))),
                signature: btoa(String.fromCharCode(...new Uint8Array(assertion.response.signature))),
                userHandle: assertion.response.userHandle
                    ? btoa(String.fromCharCode(...new Uint8Array(assertion.response.userHandle)))
                    : null
            }
        });
    }
};
