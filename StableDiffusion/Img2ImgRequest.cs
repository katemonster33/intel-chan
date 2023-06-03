using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelChan.StableDiffusion
{
    public class Img2ImgRequest
    {
        public List<string> init_images { get; set; } = new List<string>();
        public float denoising_strength { get; set; } = 0.75f;
        public string prompt { get; set; }
        public string negative_prompt { get; set; } = "";
        public int seed { get; set; } = -1;
        public int steps { get; set; } = 50;
        public int cfg_scale { get; set; } = 7;
        public bool tiling { get; set; } = false;
        public string sampler_index { get; set; } = "Euler a";
        public bool send_images { get; set; } = true;
        public bool save_images { get; set; } = false;
    }
}
