(function(){
  function toBytes(base64){
    const bin = atob(base64);
    const bytes = new Uint8Array(bin.length);
    for (let i=0;i<bin.length;i++){ bytes[i] = bin.charCodeAt(i); }
    return bytes;
  }
  function toBase64(bytes){
    let bin = "";
    const arr = new Uint8Array(bytes);
    for (let i=0;i<arr.length;i++){ bin += String.fromCharCode(arr[i]); }
    return btoa(bin);
  }
  async function importKey(keyBytes){
    return await crypto.subtle.importKey(
      'raw',
      keyBytes,
      { name: 'AES-GCM', length: keyBytes.byteLength * 8 },
      false,
      ['encrypt','decrypt']
    );
  }
  window.cryptoInterop = {
    encrypt: async function(plaintextBase64, keyBase64, ivBase64){
      const pt = toBytes(plaintextBase64);
      const keyBytes = toBytes(keyBase64);
      const iv = toBytes(ivBase64);
      if (iv.byteLength !== 12) throw new Error('AES-GCM requires 12-byte IV');
      const key = await importKey(keyBytes);
      const cipherWithTag = await crypto.subtle.encrypt({ name: 'AES-GCM', iv, tagLength: 128 }, key, pt);
      return toBase64(cipherWithTag);
    },
    decrypt: async function(cipherWithTagBase64, keyBase64, ivBase64){
      const ct = toBytes(cipherWithTagBase64);
      const keyBytes = toBytes(keyBase64);
      const iv = toBytes(ivBase64);
      if (iv.byteLength !== 12) throw new Error('AES-GCM requires 12-byte IV');
      const key = await importKey(keyBytes);
      const plaintext = await crypto.subtle.decrypt({ name: 'AES-GCM', iv, tagLength: 128 }, key, ct);
      return toBase64(plaintext);
    }
  };
})();

