using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Player cosmetic/outfit management API. Sets hats, skins, pets, nameplates,
    /// and colors on any player, respecting the game's native RPC sync flow.
    ///
    /// All setter methods are host-only by design (cosmetic RPCs are broadcast
    /// from the host). Non-host callers should send a custom RPC to the host
    /// and let the host apply the change.
    /// </summary>
    public static class CosmeticAPI
    {
        private static HatManager HatManager => HatManager.Instance;

        /// <summary>
        /// Sets the player's color. Host-only; broadcasts via RpcSetColor.
        /// </summary>
        public static void SetColor(PlayerControl player, int colorId)
        {
            if (player == null || player.Data == null) return;
            if (colorId < 0) return;

            var palette = Palette.PlayerColors;
            if (palette != null && colorId >= palette.Length) return;

            player.Data.ColorId = (byte)colorId;
            player.RpcSetColor((byte)colorId);
        }

        /// <summary>
        /// Sets the player's hat by index in HatManager.AllHats. Host-only.
        /// </summary>
        public static void SetHat(PlayerControl player, int hatIndex)
        {
            if (player == null || HatManager == null) return;
            var hats = HatManager.AllHats;
            if (hats == null || hatIndex < 0 || hatIndex >= hats.Count) return;

            var hat = hats.get_Item(hatIndex);
            if (hat == null) return;
            player.RpcSetHat(hat.ProductId);
        }

        /// <summary>
        /// Sets the player's hat by ProductId string. Host-only.
        /// </summary>
        public static void SetHatByProductId(PlayerControl player, string productId)
        {
            if (player == null || string.IsNullOrEmpty(productId)) return;
            player.RpcSetHat(productId);
        }

        /// <summary>
        /// Sets the player's skin by index in HatManager.AllSkins. Host-only.
        /// </summary>
        public static void SetSkin(PlayerControl player, int skinIndex)
        {
            if (player == null || HatManager == null) return;
            var skins = HatManager.AllSkins;
            if (skins == null || skinIndex < 0 || skinIndex >= skins.Count) return;

            var skin = skins.get_Item(skinIndex);
            if (skin == null) return;
            player.RpcSetSkin(skin.ProductId);
        }

        /// <summary>
        /// Sets the player's skin by ProductId string. Host-only.
        /// </summary>
        public static void SetSkinByProductId(PlayerControl player, string productId)
        {
            if (player == null || string.IsNullOrEmpty(productId)) return;
            player.RpcSetSkin(productId);
        }

        /// <summary>
        /// Sets the player's pet by index in HatManager.AllPets. Host-only.
        /// </summary>
        public static void SetPet(PlayerControl player, int petIndex)
        {
            if (player == null || HatManager == null) return;
            var pets = HatManager.AllPets;
            if (pets == null || petIndex < 0 || petIndex >= pets.Count) return;

            var pet = pets.get_Item(petIndex);
            if (pet == null) return;
            player.RpcSetPet(pet.ProductId);
        }

        /// <summary>
        /// Sets the player's pet by ProductId string. Host-only.
        /// </summary>
        public static void SetPetByProductId(PlayerControl player, string productId)
        {
            if (player == null || string.IsNullOrEmpty(productId)) return;
            player.RpcSetPet(productId);
        }

        /// <summary>
        /// Sets the player's nameplate/visor by index in HatManager.AllNamePlates.
        /// Host-only.
        /// </summary>
        public static void SetNamePlate(PlayerControl player, int nameplateIndex)
        {
            if (player == null || HatManager == null) return;
            var plates = HatManager.AllNamePlates;
            if (plates == null || nameplateIndex < 0 || nameplateIndex >= plates.Count) return;

            var plate = plates.get_Item(nameplateIndex);
            if (plate == null) return;
            player.RpcSetNamePlate(plate.ProductId);
        }

        /// <summary>
        /// Looks up a hat by ProductId substring (case-insensitive). Returns the
        /// first match, or null.
        /// </summary>
        public static HatBehaviour FindHat(string partialId)
        {
            if (HatManager == null) return null;
            var hats = HatManager.AllHats;
            if (hats == null) return null;

            var query = (partialId ?? "").ToLowerInvariant();
            for (int i = 0; i < hats.Count; i++)
            {
                var h = hats.get_Item(i);
                if (h == null) continue;
                var hay = ((h.ProductId ?? "") + " " + (h.StoreName ?? "")).ToLowerInvariant();
                if (hay.Contains(query)) return h;
            }
            return null;
        }

        /// <summary>
        /// Looks up a skin by ProductId substring. Returns the first match, or null.
        /// </summary>
        public static SkinLayer FindSkin(string partialId)
        {
            if (HatManager == null) return null;
            var skins = HatManager.AllSkins;
            if (skins == null) return null;

            var query = (partialId ?? "").ToLowerInvariant();
            for (int i = 0; i < skins.Count; i++)
            {
                var s = skins.get_Item(i);
                if (s == null) continue;
                var hay = ((s.ProdId ?? "") + " " + (s.StoreName ?? "")).ToLowerInvariant();
                if (hay.Contains(query)) return s;
            }
            return null;
        }

        /// <summary>
        /// Looks up a pet by ProductId substring. Returns the first match, or null.
        /// </summary>
        public static PetBehaviour FindPet(string partialId)
        {
            if (HatManager == null) return null;
            var pets = HatManager.AllPets;
            if (pets == null) return null;

            var query = (partialId ?? "").ToLowerInvariant();
            for (int i = 0; i < pets.Count; i++)
            {
                var p = pets.get_Item(i);
                if (p == null) continue;
                var hay = ((p.ProductId ?? "") + " " + (p.StoreName ?? "")).ToLowerInvariant();
                if (hay.Contains(query)) return p;
            }
            return null;
        }

        /// <summary>
        /// Returns a list of all hat ProductIds.
        /// </summary>
        public static List<string> GetAllHatIds()
        {
            var result = new List<string>();
            if (HatManager == null) return result;
            var hats = HatManager.AllHats;
            if (hats == null) return result;
            for (int i = 0; i < hats.Count; i++)
            {
                var h = hats.get_Item(i);
                if (h != null && !string.IsNullOrEmpty(h.ProductId))
                    result.Add(h.ProductId);
            }
            return result;
        }

        /// <summary>
        /// Returns a list of all skin ProductIds.
        /// </summary>
        public static List<string> GetAllSkinIds()
        {
            var result = new List<string>();
            if (HatManager == null) return result;
            var skins = HatManager.AllSkins;
            if (skins == null) return result;
            for (int i = 0; i < skins.Count; i++)
            {
                var s = skins.get_Item(i);
                if (s != null && !string.IsNullOrEmpty(s.ProdId))
                    result.Add(s.ProdId);
            }
            return result;
        }
    }
}