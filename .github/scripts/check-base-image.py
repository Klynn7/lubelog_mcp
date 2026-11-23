#!/usr/bin/env python3
"""Decide whether the published image needs a rebuild because its base image tag moved.

Compares the current digest of BASE_IMAGE (e.g. mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled) with the
org.opencontainers.image.base.digest label on PUBLISHED_IMAGE (e.g. ghcr.io/klynn7/lubelogger_mcp:edge).
Writes `digest=<sha256:...>` and `changed=true|false` to $GITHUB_OUTPUT (or stdout when run locally).
Only anonymous registry reads are used; both images are public.
"""
import json
import os
import sys
import urllib.parse
import urllib.request

INDEX_TYPES = ", ".join([
    "application/vnd.oci.image.index.v1+json",
    "application/vnd.docker.distribution.manifest.list.v2+json",
    "application/vnd.oci.image.manifest.v1+json",
    "application/vnd.docker.distribution.manifest.v2+json",
])
LABEL = "org.opencontainers.image.base.digest"


def split_ref(ref):
    registry, _, rest = ref.partition("/")
    repo, _, tag = rest.rpartition(":")
    return registry, repo, tag


def request(url, token=None, accept=INDEX_TYPES, method="GET"):
    headers = {"Accept": accept, "User-Agent": "base-image-check"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return urllib.request.urlopen(urllib.request.Request(url, headers=headers, method=method), timeout=30)


def token_for(registry, repo):
    """Anonymous pull token (GHCR needs one; MCR does not)."""
    if registry != "ghcr.io":
        return None
    scope = urllib.parse.quote(f"repository:{repo}:pull")
    with request(f"https://ghcr.io/token?scope={scope}", accept="application/json") as r:
        return json.load(r)["token"]


def current_digest(ref):
    registry, repo, tag = split_ref(ref)
    token = token_for(registry, repo)
    with request(f"https://{registry}/v2/{repo}/manifests/{tag}", token, method="HEAD") as r:
        return r.headers["Docker-Content-Digest"]


def published_base_label(ref):
    """Read LABEL from the linux/amd64 config of the published image; None if absent/unreadable."""
    registry, repo, tag = split_ref(ref)
    token = token_for(registry, repo)
    base = f"https://{registry}/v2/{repo}"
    with request(f"{base}/manifests/{tag}", token) as r:
        manifest = json.load(r)
    if "manifests" in manifest:  # multi-arch index: pick linux/amd64
        entry = next(m for m in manifest["manifests"]
                     if m.get("platform", {}).get("os") == "linux" and m["platform"].get("architecture") == "amd64")
        with request(f"{base}/manifests/{entry['digest']}", token) as r:
            manifest = json.load(r)
    with request(f"{base}/blobs/{manifest['config']['digest']}", token, accept="*/*") as r:
        config = json.load(r)
    return (config.get("config") or {}).get("Labels", {}).get(LABEL)


def main():
    base_image = os.environ["BASE_IMAGE"]
    published_image = os.environ["PUBLISHED_IMAGE"]
    digest = current_digest(base_image)  # failing here should fail the job loudly
    try:
        built_from = published_base_label(published_image)
    except Exception as ex:  # no image yet, registry hiccup: rebuild rather than silently skip
        print(f"Could not read {LABEL} from {published_image}: {ex}", file=sys.stderr)
        built_from = None
    changed = digest != built_from
    print(f"{base_image} is {digest}; {published_image} was built from {built_from}; changed={str(changed).lower()}",
          file=sys.stderr)
    lines = f"digest={digest}\nchanged={str(changed).lower()}\n"
    out = os.environ.get("GITHUB_OUTPUT")
    if out:
        with open(out, "a") as f:
            f.write(lines)
    else:
        sys.stdout.write(lines)


if __name__ == "__main__":
    main()
