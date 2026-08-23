from __future__ import annotations

import re
import sys
from pathlib import Path


BLOCK_RE = re.compile(r"^--- !u!(\d+) &(\d+)(?: stripped)?$", re.MULTILINE)


def parse_blocks(text: str):
    matches = list(BLOCK_RE.finditer(text))
    blocks = {}
    for index, match in enumerate(matches):
        start = match.end()
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        blocks[int(match.group(2))] = (int(match.group(1)), text[start:end])
    return blocks


def scalar(block: str, field: str):
    match = re.search(rf"^  {re.escape(field)}: (.*)$", block, re.MULTILINE)
    return match.group(1).strip() if match else None


def reference(block: str, field: str):
    value = scalar(block, field)
    if not value:
        return None
    match = re.search(r"fileID: (-?\d+)", value)
    return int(match.group(1)) if match else None


def main():
    source = Path(sys.argv[1])
    text = source.read_text(encoding="utf-8-sig")
    blocks = parse_blocks(text)

    game_object_names = {
        file_id: scalar(block, "m_Name")
        for file_id, (class_id, block) in blocks.items()
        if class_id == 1
    }

    component_game_objects = {
        file_id: reference(block, "m_GameObject")
        for file_id, (_, block) in blocks.items()
        if reference(block, "m_GameObject") is not None
    }

    game_object_components = {}
    transform_children = {}
    transform_game_objects = {}
    sprite_guids_by_game_object = {}
    for file_id, (class_id, block) in blocks.items():
        if class_id == 1:
            game_object_components[file_id] = [
                int(value)
                for value in re.findall(r"^  - component: \{fileID: (-?\d+)\}", block, re.MULTILINE)
            ]
        game_object_id = reference(block, "m_GameObject")
        if game_object_id is None:
            continue
        if class_id in (4, 224):
            transform_game_objects[file_id] = game_object_id
            transform_children[file_id] = [
                int(value)
                for value in re.findall(r"^  - \{fileID: (-?\d+)\}", block, re.MULTILINE)
            ]
        sprite_guids = re.findall(
            r"^  m_(?:Sprite|Texture): \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: \d+\}",
            block,
            re.MULTILINE,
        )
        if sprite_guids:
            sprite_guids_by_game_object.setdefault(game_object_id, []).extend(sprite_guids)

    transform_for_game_object = {
        game_object_id: component_id
        for game_object_id, components in game_object_components.items()
        for component_id in components
        if component_id in transform_game_objects
    }

    guid_paths = {}
    for meta_path in source.parents[2].joinpath("Assets").rglob("*.meta"):
        try:
            meta_text = meta_path.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            continue
        guid_match = re.search(r"^guid: ([0-9a-f]{32})$", meta_text, re.MULTILINE)
        if guid_match:
            guid_paths[guid_match.group(1)] = str(meta_path.with_suffix(""))

    def image_paths(game_object_id: int):
        paths = []
        visited = set()

        def visit(current_game_object_id):
            if current_game_object_id in visited:
                return
            visited.add(current_game_object_id)
            for guid in sprite_guids_by_game_object.get(current_game_object_id, []):
                path = guid_paths.get(guid, guid)
                if path not in paths:
                    paths.append(path)
            transform_id = transform_for_game_object.get(current_game_object_id)
            for child_transform_id in transform_children.get(transform_id, []):
                child_game_object_id = transform_game_objects.get(child_transform_id)
                if child_game_object_id is not None:
                    visit(child_game_object_id)

        visit(game_object_id)
        return paths

    story_match = re.search(
        r"^--- !u!114 &(\d+)\r?\nMonoBehaviour:.*?"
        r"m_EditorClassIdentifier: BBSGame\.Story::StoryFlowController\r?\n"
        r"(?P<body>.*?)(?=^--- !u!)",
        text,
        re.MULTILINE | re.DOTALL,
    )
    if not story_match:
        raise SystemExit("StoryFlowController block not found")

    body = story_match.group("body")
    round_index = -1
    post_index = -1
    current_button = None
    in_posts = False

    for line in body.splitlines():
        if line.startswith("  - mail:"):
            round_index += 1
            post_index = -1
            in_posts = False
            continue
        if line == "    posts:":
            in_posts = True
            continue
        if line.startswith("    selectionPost:"):
            in_posts = False
            continue
        post_match = re.match(r"    - button: \{fileID: (-?\d+)\}", line)
        if post_match and in_posts:
            post_index += 1
            continue
        content_match = re.match(r"      contentImage: \{fileID: (-?\d+)\}", line)
        if content_match and in_posts:
            content_game_object_id = int(content_match.group(1))
            print(
                f"CONTENT\tROUND={round_index + 1}\tPOST={post_index + 1}\t"
                f"OBJECT={game_object_names.get(content_game_object_id) or '?'}\t"
                f"IMAGES={' | '.join(image_paths(content_game_object_id)) or '?'}"
            )
            continue
        collectible_button_match = re.match(r"      - button: \{fileID: (-?\d+)\}", line)
        if collectible_button_match:
            current_button = int(collectible_button_match.group(1))
            continue
        item_match = re.match(r"        itemId: (.+)", line)
        if item_match and current_button is not None:
            game_object_id = component_game_objects.get(current_button)
            game_object_name = game_object_names.get(game_object_id)
            print(
                f"ROUND={round_index + 1}\tPOST={post_index + 1}\t"
                f"ITEM={item_match.group(1).strip()}\tBUTTON={game_object_name or '?'}\t"
                f"BUTTON_ID={current_button}"
            )


if __name__ == "__main__":
    main()
