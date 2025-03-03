﻿using CORERenderer.shaders;

namespace CORERenderer.OpenGL
{
    //commonly used shaders are placed in strings here, so that theyre neatly kept together and reduced the amount of files
    public class GenericShaders
    {
        private static Shader image2DShader, lightingShader, backgroundShader, gridShader, GenericLightingShader, solidColorQuadShader, arrowShader, pickShader, framebufferShader, bonelessPickShader, cubemapShader, skyboxShader, PBRShader, normalVisualisationShader, shadowShader, water;

        public static Shader Image2D { get => image2DShader; }
        public static Shader Light { get => lightingShader; }
        public static Shader Background { get => backgroundShader; }
        public static Shader Grid { get => gridShader; }
        public static Shader Lighting { get => GenericLightingShader; }
        public static Shader Quad { get => solidColorQuadShader; }
        public static Shader Arrow { get => arrowShader; }
        public static Shader IDPicking { get => pickShader; }
        public static Shader Framebuffer { get => framebufferShader; }
        public static Shader BonelessPickShader { get => bonelessPickShader; }
        public static Shader Cubemap { get => cubemapShader; }
        public static Shader Skybox { get => skyboxShader; }
        //public static Shader PBR { get => PBRShader; }
        public static Shader NormalVisualisation { get => normalVisualisationShader; }
        public static Shader Shadow { get => shadowShader; }
        public static Shader Water { get => water; }

        public static string Log
        {
            get
            {
                string returnString = image2DShader.StartLog;
                returnString += $"\n{lightingShader.StartLog}";
                returnString += $"\n{backgroundShader.StartLog}";
                returnString += $"\n{gridShader.StartLog}";
                returnString += $"\n{GenericLightingShader.StartLog}";
                returnString += $"\n{solidColorQuadShader.StartLog}";
                returnString += $"\n{arrowShader.StartLog}";
                returnString += $"\n{pickShader.StartLog}";
                returnString += $"\n{framebufferShader.StartLog}";
                returnString += $"\n{bonelessPickShader.StartLog}";
                returnString += $"\n{cubemapShader.StartLog}";
                returnString += $"\n{skyboxShader.StartLog}";
                //returnString += $"\n{PBRShader.StartLog}";
                returnString += $"\n{normalVisualisationShader.StartLog}";
                returnString += $"\n{shadowShader.StartLog}";
                return returnString;
            }
        }

        internal static void SetShaders()
        {
            image2DShader = new(image2DVertText, image2DFragText);
            lightingShader = new(lightVertText, lightFragText);
            backgroundShader = new(backgroundVertText, backgroundFragText);
            gridShader = new(gridVertText, gridFragText);
            if (Rendering.shaderConfig == ShaderType.PathTracing)
                GenericLightingShader = new(defaultVertexShaderText, pathTracingFragText, pathTracingGeomText);
            else if (Rendering.shaderConfig == ShaderType.PBR)
                GenericLightingShader = new(defaultVertexShaderText, PBRFragText);
            else if (Rendering.shaderConfig == ShaderType.FullBright)
                GenericLightingShader = new(defaultVertexShaderText, fullBrightFragText);
            solidColorQuadShader = new(quadVertText, quadFragText);
            arrowShader = new(arrowVertText, arrowFragText);
            pickShader = new(defaultVertexShaderText, quadFragText);
            framebufferShader = new(defaultFrameBufferVertText, defaultFrameBufferFragText);
            bonelessPickShader = new(pickVertexShader, arrowFragText);
            cubemapShader = new(cubemapVert, cubemapFrag);
            skyboxShader = new(skyboxVert, skyboxFrag);
            //PBRShader = new(defaultVertexShaderText, PBRFragText);
            normalVisualisationShader = new(normalVisVertText, normalVisFragText, normalVisGeomText);
            shadowShader = new(shadowVertText, shadowFragText/*, shadowGeomText*/);
        }

        private static string shadowVertText =
            """
            #version 430 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTexCoords;
            layout (location = 2) in vec3 aNormal;
            layout (location = 3) in ivec4 bonesID1;
            layout (location = 4) in ivec4 bonesID2;
            layout (location = 5) in vec4 weights1;
            layout (location = 6) in vec4 weights2;

            uniform mat4 model;
            uniform mat4 projection;
            uniform mat4 view;

            out vec4 FragPos;

            void main()
            {
                FragPos = vec4(aPos, 1) * model;
                gl_Position = FragPos * view * projection;
            }  
            """;

        private static string shadowGeomText =
            """
            #version 430 core
            layout (triangles) in;
            layout (triangle_strip, max_vertices=18) out;

            uniform mat4 shadowMatrices[6];
            uniform mat4 projection;

            out vec4 FragPos;

            void main()
            {
                for(int face = 0; face < 6; face++)
                {
                    gl_Layer = face;
                    for(int i = 0; i < 3; i++)
                    {
                        FragPos = gl_in[i].gl_Position;
                        gl_Position = FragPos * shadowMatrices[face] * projection;
                        EmitVertex();
                    }    
                    EndPrimitive();
                }
            }  
            """;

        private static string shadowFragText =
            """
            #version 430 core
            out vec4 FragColor;

            in vec4 FragPos;

            uniform vec3 lightPos;
            uniform float farPlane;

            void main()
            {
                /*// get distance between fragment and light source
                float lightDistance = length(FragPos.xyz - lightPos);

                // map to [0;1] range by dividing by far_plane
                lightDistance = lightDistance / farPlane;

                // write this as modified depth
                FragColor = vec4(vec3(lightDistance), 1);
                gl_FragDepth = lightDistance;*/
            } 
            """;

        private static string normalVisVertText =
            """
            #version 430
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTexCoords;
            layout (location = 2) in vec3 aNormal;
            layout (location = 3) in ivec4 bonesID1;
            layout (location = 4) in ivec4 bonesID2;
            layout (location = 5) in vec4 weights1;
            layout (location = 6) in vec4 weights2;
            
            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            out VS_OUT 
            {
                vec3 normal;
                vec2 texCoords;
                mat4 model;
            } vs_out;

            uniform mat4 model;

            void main() 
            {
                vec4 pos = (vec4(aPos, 1) * model);
            
            	vs_out.normal = mat3(transpose(inverse(model))) * aNormal; //way more efficient if calculated on CPU
                vs_out.texCoords = aTexCoords;
                vs_out.model = model;

            	gl_Position = pos;
            }
            """;

        private static string normalVisGeomText =
            """
            #version 430 core
            layout (triangles) in;
            layout (line_strip, max_vertices = 6) out;

            in VS_OUT {
                vec3 normal;
                vec2 texCoords;
                mat4 model;
            } gs_in[];

            const float MAGNITUDE = 0.15;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            void GenerateLine(int index)
            {
                gl_Position = gl_in[index].gl_Position * view * projection;
                EmitVertex();
                gl_Position = (gl_in[index].gl_Position + vec4(gs_in[index].normal, 0.0) * MAGNITUDE) * view * projection;
                EmitVertex();
                EndPrimitive();
            }

            void main()
            {
                GenerateLine(0); // first vertex normal
                GenerateLine(1); // second vertex normal
                GenerateLine(2); // third vertex normal
            }
            """;

        private static string normalVisFragText =
            """
            #version 430 core
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(1.0, 0.0, 1.0, 1.0);
            }  
            """;
        //a lot of code used from LearnOpenGL at https://github.com/JoeyDeVries/LearnOpenGL/blob/master/src/6.pbr/1.2.lighting_textured/1.2.pbr.fs
        private static string PBRFragText =
            """
            #version 430 core
            out vec4 FragColor;

            uniform vec3 viewPos;
            uniform vec3 lightPos[2];

            uniform float farPlane;

            in vec2 TexCoords;

            in vec3 Normal;
            in vec3 FragPos;


            // material parameters
            uniform sampler2D albedoMap;
            uniform sampler2D normalMap;
            uniform sampler2D metallicMap;
            uniform sampler2D roughnessMap;
            uniform sampler2D aoMap;
            uniform sampler2D heightMap;
            uniform sampler2D alphaMap;

            uniform samplerCube shadowMap;
            uniform samplerCube reflectionCubemap;
            uniform samplerCube irradianceMap;

            uniform int isHighlighted;
            uniform int lowQuality;

            const float PI = 3.14159265359;
            #define heightScale 0.1

            float GetShadow(vec3 FragPos)
            {
                vec3 fragToLight = FragPos - lightPos[0];
                float currentDepth = length(fragToLight);

                float shadow = 0.0;
                float bias = 0.05;
                float samples = 1.0;
                float offset = 0.1;
                for(float x = -offset; x < offset; x += offset / (samples * 0.5))
                {
                    for(float y = -offset; y < offset; y += offset / (samples * 0.5))
                    {
                        for(float z = -offset; z < offset; z += offset / (samples * 0.5))
                        {
                            float closestDepth = texture(shadowMap, fragToLight + vec3(x, y, z)).r;
                            closestDepth *= farPlane; // undo mapping [0;1]
                            if(currentDepth - bias > closestDepth)
                                shadow += 1.0;
                        }
                    }
                }
                shadow /= (samples * samples * samples);
                return shadow;
            }

            //from https://github.com/DOWNPOURDIGITAL/glsl-parallax-occlusion-mapping, all credit there
            vec2 ParallaxOcclusionMapping( sampler2D depthMap, vec2 uv, vec2 displacement, float pivot ) {
            	const float layerDepth = 1.0 / float(8);
            	float currentLayerDepth = 0.0;

            	vec2 deltaUv = displacement / float(8);
            	vec2 currentUv = uv + pivot * displacement;
            	float currentDepth = texture2D( depthMap, currentUv ).r;

            	for( int i = 0; i < 8; i++ ) {
            		if( currentLayerDepth > currentDepth )
            			break;

            		currentUv -= deltaUv;
            		currentDepth = texture2D( depthMap, currentUv ).r;
            		currentLayerDepth += layerDepth;
            	}

            	vec2 prevUv = currentUv + deltaUv;
            	float endDepth = currentDepth - currentLayerDepth;
            	float startDepth =
            		texture2D( depthMap, prevUv ).r - currentLayerDepth + layerDepth;

            	float w = endDepth / ( endDepth - startDepth );

            	return mix( currentUv, prevUv, w );
            }


            vec2 ParallaxOcclusionMapping( sampler2D depthMap, vec2 uv, vec2 displacement ) {
            	return ParallaxOcclusionMapping( depthMap, uv, displacement, 0.0 );
            }
            
            #define Directions 16.0

            vec3 GetGuassianBlur(float roughness, vec3 viewDir, vec3 normal)
            {
                vec3 dir = reflect(-viewDir, normal);
                vec3 highestReflection = textureLod(reflectionCubemap, dir, 4 * roughness).rgb;
                vec3 lowestReflection = texture(irradianceMap, dir).rgb;
                return ((1 - roughness) * lowestReflection + roughness * highestReflection) / 2;//(lowestReflection - highestReflection) * roughness + highestReflection; //interpolate between the 2 colors based on the roughness. Add the highest reflection last, because a roughness of 0 means its as smooth as can be
            }

            #define FresnelExponent 5

            vec3 GetFresnelOutline(vec3 normal, vec3 viewPos)
            {
                vec3 color = vec3(.9, .2, .9);

                float fresnel = dot(normal, viewPos);
                fresnel = clamp(1 - fresnel, 0.0, 1.0);
                fresnel = pow(fresnel, FresnelExponent);
                vec3 finalColor = color * fresnel;
                return finalColor;
            }

            vec3 GetAmbientFromMap(vec3 texCoords)
            {
                return texture(irradianceMap, texCoords).rgb;
            }

            vec3 getNormalFromMap(vec2 texCoords)
            {
                vec3 tangentNormal = texture(normalMap, texCoords).xyz * 2.0 - 1.0;

                vec3 Q1  = dFdx(FragPos);
                vec3 Q2  = dFdy(FragPos);
                vec2 st1 = dFdx(texCoords);
                vec2 st2 = dFdy(texCoords);

                vec3 N   = normalize(Normal);
                vec3 T  = normalize(Q1*st2.t - Q2*st1.t);
                vec3 B  = -normalize(cross(N, T));
                mat3 TBN = mat3(T, B, N);

                return normalize(TBN * tangentNormal);
            }
            
            float DistributionGGX(vec3 N, vec3 H, float roughness)
            {
                float a = roughness*roughness;
                float a2 = a*a;
                float NdotH = max(dot(N, H), 0.0);
                float NdotH2 = NdotH*NdotH;

                float nom   = a2;
                float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                denom = PI * denom * denom;

                return nom / denom;
            }
            
            float GeometrySchlickGGX(float NdotV, float roughness)
            {
                float r = (roughness + 1.0);
                float k = (r*r) / 8.0;

                float nom   = NdotV;
                float denom = NdotV * (1.0 - k) + k;

                return nom / denom;
            }
            
            float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
            {
                float NdotV = max(dot(N, V), 0.0);
                float NdotL = max(dot(N, L), 0.0);
                float ggx2 = GeometrySchlickGGX(NdotV, roughness);
                float ggx1 = GeometrySchlickGGX(NdotL, roughness);

                return ggx1 * ggx2;
            }
            
            vec3 fresnelSchlick(float cosTheta, vec3 F0)
            {
                return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
            }
            
            vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
            {
                return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
            }  

            void main()
            {		
                vec2 texCoords = ParallaxOcclusionMapping(heightMap, TexCoords, normalize(FragPos - viewPos).xy * .001);
                if(texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0)
                    discard;

                vec3 albedo     = pow(texture(albedoMap, texCoords).rgb, vec3(2.2));
                //each uses a different color channel so that its compatible with ARM maps
                float metallic  = texture(metallicMap, texCoords).b;
                float roughness = texture(roughnessMap, texCoords).g;
                float ao        = texture(aoMap, texCoords).r;
                
                vec3 N = getNormalFromMap(texCoords);
                vec3 V = normalize(viewPos - FragPos);

                vec3 reflection = GetGuassianBlur(roughness, V, N);

                // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
                // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
                vec3 F0 = vec3(0.04); 
                F0 = mix(F0, albedo, metallic);

                // reflectance equation
                vec3 Lo = vec3(0.0);
                for(int i = 0; i < 1; ++i) 
                {
                    // calculate per-light radiance
                    vec3 L = normalize(lightPos[i] - FragPos);
                    vec3 H = normalize(V + L);
                    float distance = length(lightPos[i] - FragPos);
                    float attenuation = 1.0 / (distance * distance);
                    vec3 radiance = vec3(1) * attenuation;

                    // Cook-Torrance BRDF
                    float NDF = DistributionGGX(N, H, roughness);   
                    float G   = GeometrySmith(N, V, L, roughness);      
                    vec3 F    = fresnelSchlick(max(dot(H, V), 0.0), F0);

                    vec3 numerator    = NDF * G * F; 
                    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
                    vec3 specular = numerator / denominator;

                    vec3 kS = F;
                    
                    vec3 kD = vec3(1.0) - kS;
                    
                    kD *= 1.0 - metallic;	  

                    // scale light by NdotL
                    float NdotL = max(dot(N, L), 0.0);        

                    // add to outgoing radiance Lo
                    Lo += (kD * albedo / PI + specular) * radiance * NdotL;  // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
                }   
                vec3 F = fresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);

                vec3 kS = F;
                vec3 kD = 1.0 - kS;
                kD *= 1.0 - metallic;	  

                vec3 irradiance = texture(irradianceMap, N).rgb;
                vec3 diffuse      = irradiance * albedo;
                vec3 ambient = (kD * diffuse + reflection * F * metallic * irradiance) * ao;

                vec3 color = ambient + Lo;
                if (isHighlighted == 1)
                    color += GetFresnelOutline(N, V);

                // HDR tonemapping
                color /= (color + vec3(1.0));
                // gamma correct
                color = pow(color, vec3(1.0/2.2));
                
                FragColor = vec4(color, /*texture(alphaMap, texCoords).r*/1.0);
            }
            """;

        //default shader source codes here
        private static string skyboxFrag =
            """
            #version 430 core
            out vec4 FragColor;

            in vec3 TexCoords;
            uniform samplerCube cubemap;

            void main()
            {
            	FragColor = texture(cubemap, TexCoords);
            }
            """;

        private static string skyboxVert =
            """
                        #version 430 core

            out vec3 TexCoords;

            layout (std140, binding = 0) uniform Matrices
            {
                mat4 projection;
                mat4 view;
                mat4 translationlessView;
            };

            vec3 coordinates[36] = vec3[](
            	    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),

                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),

                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),

                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f, -1.0f,  1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),

                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),

                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3( 1.0f, -1.0f,  1.0f)
            );

            void main()
            {
            	TexCoords = coordinates[gl_VertexID];
                vec4 temp =  vec4(TexCoords, 1) * translationlessView * projection;
            	gl_Position = temp.xyww;
            }
            """;

        private static string cubemapFrag =
            """
            #version 430 core
            out vec4 FragColor;

            in vec2 texCoords;

            uniform sampler2D environmentMap;

            void main()
            {    
                FragColor = texture(environmentMap, texCoords);
            }
            """;

        private static string cubemapVert =
            """
            #version 430 core
            layout (std140, binding = 0) uniform Matrices
            {
                mat4 projection;
                mat4 view;
                mat4 translationlessView;
            };

            out vec2 texCoords;

            vec3 coordinates[36] = vec3[](
                    vec3(-1.0,  1.0, -1.0),
                    vec3(-1.0, -1.0, -1.0),
                    vec3( 1.0, -1.0, -1.0),
                    vec3( 1.0, -1.0, -1.0),
                    vec3( 1.0,  1.0, -1.0),
                    vec3(-1.0,  1.0, -1.0),

                    vec3(-1.0, -1.0,  1.0),
                    vec3(-1.0, -1.0, -1.0),
                    vec3(-1.0,  1.0, -1.0),
                    vec3(-1.0,  1.0, -1.0),
                    vec3(-1.0,  1.0,  1.0),
                    vec3(-1.0, -1.0,  1.0),

                    vec3( 1.0, -1.0, -1.0),
                    vec3( 1.0, -1.0,  1.0),
                    vec3( 1.0,  1.0,  1.0),
                    vec3( 1.0,  1.0,  1.0),
                    vec3( 1.0,  1.0, -1.0),
                    vec3( 1.0, -1.0, -1.0),

                    vec3(-1.0, -1.0,  1.0),
                    vec3(-1.0,  1.0,  1.0),
                    vec3( 1.0,  1.0,  1.0),
                    vec3( 1.0,  1.0,  1.0),
                    vec3( 1.0, -1.0,  1.0),
                    vec3(-1.0, -1.0,  1.0),

                    vec3(-1.0,  1.0, -1.0),
                    vec3( 1.0,  1.0, -1.0),
                    vec3( 1.0,  1.0,  1.0),
                    vec3( 1.0,  1.0,  1.0),
                    vec3(-1.0,  1.0,  1.0),
                    vec3(-1.0,  1.0, -1.0),

                    vec3(-1.0, -1.0, -1.0),
                    vec3(-1.0, -1.0,  1.0),
                    vec3( 1.0, -1.0, -1.0),
                    vec3( 1.0, -1.0, -1.0),
                    vec3(-1.0, -1.0,  1.0),
                    vec3( 1.0, -1.0,  1.0)
            );

            void main()
            {
                texCoords = coordinates[gl_VertexID].xy;    
                gl_Position = (vec4(coordinates[gl_VertexID], 1.0) * mat4(mat3(view)) * projection).xyww;
            }
            """;

        private static string fullBrightFragText =
            """
            #version 430 core
            out vec4 FragColor;

            uniform sampler2D albedoMap;

            in vec2 TexCoords;
            uniform float transparency;

            void main()
            {
                vec3 color = texture(albedoMap, TexCoords).rgb;
                FragColor = vec4(color, 1);
                //if (transparency != 0)
                //    FragColor.a = transparency;
                //if (FragColor.a < 0.1)
                //    discard;
            }
            """;

        private static string pathTracingFragText =
            """
            #version 430 core
            out vec4 FragColor;

            struct PointLight
            {
            	vec3 position;
            	vec3 ambient;
            	vec3 diffuse;
            	vec3 specular;

            	float constant;
            	float linear;
            	float quadratic;
            };
            #define NR_POINTS_LIGHTS 1
            uniform PointLight pointLights[NR_POINTS_LIGHTS];

            struct Material 
            {
            	sampler2D Texture;
            	sampler2D diffuse;
            	sampler2D specular;
            	sampler2D normalMap;
            	float shininess;
            };
            uniform Material material;

            in vec3 overrideColor;
            in vec3 Normal;

            void main()
            {
                FragColor = vec4(overrideColor, 1);
            }
            """;

        private static string pathTracingGeomText =
            """
            #version 430 core
            layout(triangles) in;
            layout(triangle_strip, max_vertices = 3) out;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            #define PI 3.14159265

            const float EPSILON = 0.0000001;
            out vec3 overrideColor;
            out vec3 Normal;
            struct Ray
            {
            	vec3 origin;
            	vec3 direction;
            };

            struct Light
            {
            	vec3 position;
            	vec3 color;
            };
            #define NUMBER_OF_LIGHTS 1
            uniform Light lights;

            in VS_OUT
            {
            	vec3 position;
            	vec3 normal;
            } gs_in[];

            uniform Ray RAY;
            uniform int isReflective;
            uniform vec3 emission;
            uniform float sampleAmount;

            bool RayIntersects(Ray ray, out vec3 intersection);
            vec3 Radiance(Ray ray, int depth, int includeEmissiveColor);
            float GetTFromIntersection(Ray ray, vec3 intersection);
            bool IntersectsSphere(Ray ray, out vec3 intersection);
            uint GetPCGHash(inout uint seed);
            float GetRandomFloat01();

            uint randomSeed;

            void main()
            {
            	float samples = sampleAmount;
            	if (samples > 7)
            	samples = 7;

            	randomSeed = 10546 * 1973 + 543543 * 9277 + 2699 | 1;

            	Normal = gs_in[0].normal;

            	int amount = 0;
            	vec3 localColor = vec3(0);
            	for (int i = 0; i < 7; i++, amount++)
            	{
            		vec3 intersection = vec3(0, 0, 0);
            		vec3 newIntersection = vec3(0, 0, 0);
            		Ray local;
            		local.origin = RAY.origin;
            		if (i < 3)
            			local.direction = gs_in[i].position - RAY.origin;
            		else if (i == 3)
            			local.direction = normalize((gs_in[1].position - gs_in[0].position) / 2);
            		else if (i == 4)
            			local.direction = normalize((gs_in[2].position - gs_in[1].position) / 2);
            		else if (i == 5)
            			local.direction = normalize((gs_in[0].position - gs_in[2].position) / 2);
            		else if (i == 6)
            			local.direction = normalize((gs_in[1].position - gs_in[0].position) / 2 + gs_in[2].position - gs_in[0].position);
            		bool success = RayIntersects(local, intersection);
            		if (!success) continue;

            		Ray newRay;
            		newRay.origin = intersection;
            		newRay.direction = -RAY.direction + 2 * Normal * dot(RAY.direction, Normal); //standard reflection formula

            		if (IntersectsSphere(newRay, newIntersection))
            			localColor += vec3(1, 1, 1);
            		else
            			localColor += vec3(0, 0, 0);

            		amount++;
            	}
            	overrideColor = localColor / amount;

            	gl_Position = gl_in[0].gl_Position;
            	EmitVertex();
            	gl_Position = gl_in[1].gl_Position;
            	EmitVertex();
            	gl_Position = gl_in[2].gl_Position;
            	EmitVertex();
            	EndPrimitive();
            }

            bool IntersectsSphere(Ray ray, out vec3 intersection)
            {
            	float rad = 3;
            	vec3 sphereCentre = RAY.origin; //set camera pos to ray origin out of laziness, making the camera a light
            	vec3 op = sphereCentre - ray.origin;
            	float t = 1e-4;
            	float eps = 1e-4;
            	float b = dot(op, ray.direction);
            	float det = b * b - dot(op, op) + rad * rad;
            	if (det < 0)
            	{
            		intersection = vec3(0);
            		return false;
            	}
            	else
            		det = sqrt(det);

            	float option1 = b - det;
            	float option2 = b + det;
            	float result = option1 > eps ? t : option2 > eps ? t : 0;

            	intersection = ray.origin + result * ray.direction;
            	return true;
            }

            bool RayIntersects(Ray ray, out vec3 intersection)
            {
            	vec3 vertex0 = gs_in[0].position.xyz;
            	vec3 vertex1 = gs_in[1].position.xyz;
            	vec3 vertex2 = gs_in[2].position.xyz;

            	vec3 edge0 = vertex1 - vertex0;
            	vec3 edge1 = vertex2 - vertex0;

            	vec3 h = cross(ray.direction, edge1);
            	float a = dot(edge0, h);

            	if (a > -EPSILON && a < EPSILON)
            	{
            		intersection = vec3(0);
            		return false;
            	}
            	float f = 1 / a;
            	vec3 s = ray.origin - vertex0;
            	float u = f * dot(s, h);

            	if (u < 0 || u > 1)
            	{
            		intersection = vec3(0);
            		return false;
            	}

            	vec3 q = cross(s, edge0);
            	float v = f * dot(ray.direction, q);

            	if (v < 0 || v > 1)
            	{
            		//intersection = vec3(0);
            		return false;
            	}

            	float t = f * dot(edge1, q);
            	if (t > EPSILON)
            	{
            		intersection = ray.origin + ray.direction * t;
            		return true;
            	}
            	else
            	{
            		intersection = vec3(0);
            		return false;
            	}
            }

            float GetTFromIntersection(Ray ray, vec3 intersection)
            {
            	return ((intersection - ray.origin) / ray.direction).x; 
            }

            //from https://github.com/BoyBaykiller/OpenTK-PathTracer/blob/master/OpenTK-PathTracer/res/shaders/PathTracing/compute.glsl
            uint GetPCGHash(inout uint seed)
            {
                seed = seed * 747796405u + 2891336453u;
                uint word = ((seed >> ((seed >> 28u) + 4u)) ^ seed) * 277803737u;
                return (word >> 22u) ^ word;
            }

            float GetRandomFloat01()
            {
                return float(GetPCGHash(randomSeed)) / 4294967296.0;
            }
            """;

        private static string arrowVertText =
            """
            #version 430 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTexCoords;
            layout (location = 2) in vec3 aNormal;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            uniform mat4 model;

            void main() 
            {
            	gl_Position = vec4(aPos, 1) * model * view * projection;
            }
            """;

        private static string arrowFragText =
            """
            #version 430 core
            out vec4 FragColor;

            uniform vec3 color;

            void main() 
            {
            	FragColor = vec4(color, 1);
            }
            """;

        private static string lightVertText =
            """
            #version 430 core
            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            	mat4 translationlessView;
            };

            vec3 coordinates[36] = vec3[](
            	vec3(-0.5, -0.5, -0.5), vec3(0.5, 0.5, -0.5), vec3(0.5, -0.5, -0.5),
                vec3(0.5, 0.5, -0.5), vec3(-0.5, -0.5, -0.5), vec3(-0.5, 0.5, -0.5),

            	vec3(-0.5, -0.5, 0.5), vec3(0.5, -0.5, 0.5), vec3(0.5, 0.5, 0.5),
                vec3(0.5, 0.5, 0.5), vec3(-0.5, 0.5, 0.5), vec3(-0.5, -0.5, 0.5),

            	vec3(-0.5, 0.5, 0.5), vec3(-0.5, 0.5, -0.5), vec3(-0.5, -0.5, -0.5),
                vec3(-0.5, -0.5, -0.5), vec3(-0.5, -0.5, 0.5), vec3(-0.5, 0.5, 0.5),

            	vec3(0.5, 0.5, 0.5), vec3(0.5, -0.5, -0.5), vec3(0.5, 0.5, -0.5),
                vec3(0.5, -0.5, -0.5), vec3(0.5, 0.5, 0.5), vec3(0.5, -0.5, 0.5),

            	vec3(-0.5, -0.5, -0.5), vec3(0.5, -0.5, -0.5), vec3(0.5, -0.5, 0.5),
                vec3(0.5, -0.5, 0.5), vec3(-0.5, -0.5, 0.5), vec3(-0.5, -0.5, -0.5),

            	vec3(-0.5, 0.5, -0.5), vec3(0.5, 0.5, 0.5), vec3(0.5, 0.5, -0.5),
                vec3(0.5, 0.5, 0.5), vec3(-0.5, 0.5, -0.5), vec3(-0.5, 0.5, 0.5)
            );

            uniform mat4 model;

            void main() 
            {
            	vec3 v1 = coordinates[gl_VertexID].xyz;
            	gl_Position = vec4(v1, 1.0) * model * view * projection;
            }
            """;

        private static string lightFragText =
            """
            #version 430 core
            out vec4 FragColor;

            void main() 
            {
            	FragColor = vec4(1.0);
            }
            """;

        private static string image2DVertText =
            """
            #version 430 core
            layout (location = 0) in vec4 vertex;

            out vec2 TexCoords;

            uniform mat4 projection;

            void main()
            {
                gl_Position = projection * vec4(vertex.xy, 0, 1);
                TexCoords = vertex.zw;
            }
            """;

        private static string image2DFragText =
            """
            #version 430 core
            out vec4 FragColor;

            in vec2 TexCoords;

            uniform sampler2D Texture;

            void main()
            {
            	FragColor = texture(Texture, TexCoords);
            }
            """;

        private static string gridVertText =
            """
            #version 430 core
            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            	mat4 translationlessView;
            };

            out vec3 coor;
            out vec3 oPlayerPos;

            out mat4 oModel;
            out mat4 oView;
            out mat4 oProjection;

            uniform mat4 model;
            uniform vec3 playerPos;

            vec3 coordinates[6] = vec3[](
            	vec3(-10, 0, -10), vec3(10, 0, 10), vec3(10, 0, -10),
                vec3(10, 0, 10), vec3(-10, 0, -10), vec3(-10, 0, 10)
            );

            void main() 
            {
            	oModel = model;
            	oView = view;
            	oProjection = projection;
            	oPlayerPos = playerPos;

            	vec3 v1 = coordinates[gl_VertexID].xyz;
            	coor = v1;
            	gl_Position = vec4(v1, 1.0) * model * view * projection;
            }
            """;

        private static string gridFragText =
            """
            #version 430 core
            out vec4 gridColor;

            in mat4 oModel;
            in mat4 oView;
            in mat4 oProjection;

            in vec3 coor;
            in vec3 oPlayerPos;

            in vec3 nearPoint;
            in vec3 farPoint;

            float near = 0.01;
            float far = 100;
            float opacity;

            vec4 grid(vec3 fragPos3D, float scale, bool drawAxis) {
                vec2 coord = fragPos3D.xz * scale;
                vec2 derivative = fwidth(coord);
                vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
                float line = min(grid.x, grid.y);
                float minimumz = min(derivative.y, 1);
                float minimumx = min(derivative.x, 1);
                vec4 color = vec4(0.1, 0.1, 0.1, 1.0 - min(line, 1.0));

                if (fragPos3D.x > -0.001 * minimumx && fragPos3D.x < 0.001 * minimumx)
                    color.z = 0.7;

                if (fragPos3D.z > -0.001 * minimumz && fragPos3D.z < 0.001 * minimumz)
                    color.x = 0.7;

                return color;
            }

            void main() 
            {
                vec3 coor3D = (vec4(coor, 1) * oModel).xyz;

                float Distance = distance(oPlayerPos, coor);
                //float opacity = clamp(Distance / length(oPlayerPos + coor3D) * 2, 0, 1);

                gridColor = grid(coor, 1000, true) + grid(coor, 100, true);
                if (gridColor.a == 0)
                    discard;
                gridColor.a *= exp(-distance(vec3(oPlayerPos.x, 0, oPlayerPos.z), vec3(coor3D.x, 0, coor3D.z)) * 0.03);
                //gridColor.a *= opacity;
            }
            """;

        private static string backgroundVertText =
            """
            #version 430 core

            out vec3 TexCoords;

            layout (std140, binding = 0) uniform Matrices
            {
                mat4 projection;
                mat4 view;
                mat4 translationlessView;
            };

            vec3 coordinates[36] = vec3[](
            	    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),

                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),

                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),

                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f, -1.0f,  1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),

                    vec3(-1.0f,  1.0f, -1.0f),
                    vec3( 1.0f,  1.0f, -1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3( 1.0f,  1.0f,  1.0f),
                    vec3(-1.0f,  1.0f,  1.0f),
                    vec3(-1.0f,  1.0f, -1.0f),

                    vec3(-1.0f, -1.0f, -1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3( 1.0f, -1.0f, -1.0f),
                    vec3(-1.0f, -1.0f,  1.0f),
                    vec3( 1.0f, -1.0f,  1.0f)
            );

            void main()
            {
            	TexCoords = coordinates[gl_VertexID];
                vec4 temp =  vec4(TexCoords, 1) * mat4(mat3(view)) * projection;
            	gl_Position = temp.xyww;
            }
            """;

        private static string backgroundFragText =
            """
            #version 430 core
            out vec4 FragColor;
            in vec3 TexCoords;

            uniform samplerCube environmentMap;

            void main()
            {		
                vec3 envColor = texture(environmentMap, TexCoords).rgb;
                FragColor = vec4(envColor, 1.0);
            }
            """;

        private static string quadVertText =
            """
            #version 430 core
            layout (location = 0) in vec2 aPos;

            uniform mat4 projection;

            void main()
            {
            	gl_Position = projection * vec4(aPos.xy, 0, 1);
            }
            """;

        private static string quadFragText =
            """
            #version 430 core
            out vec4 FragColor;

            uniform vec3 color;
            uniform float alpha;

            void main()
            {
            	float alpha2 = alpha;
            	if (alpha == 0)
            		alpha2 = 1;

            	FragColor = vec4(color, alpha2);
            }
            """;

        private static string defaultFrameBufferVertText =
            """
            #version 430 core
            layout (location = 0) in vec2 aPos;
            layout (location = 1) in vec2 aTexCoords;

            out vec2 TexCoords;

            void main()
            {
            	gl_Position = vec4(aPos.x, aPos.y, 0, 1);
            	TexCoords = aTexCoords;
            }
            """;

        private static string defaultFrameBufferFragText =
            """
            #version 430 core
            out vec4 FragColor;

            in vec2 TexCoords;

            uniform sampler2D screenTexture;
            uniform sampler2D depthTexture;

            uniform int mipMapCount;

            uniform int useVignette;
            uniform float vignetteStrength;

            uniform int useChromaticAberration;
            uniform vec3 chromAberIntensities;

            uniform int useDOF;
            uniform float DOFStrength;
            uniform float DOFFocusPoint;

            uniform float farPlane;
            uniform float nearPlane;

            void main()
            {
            	float distanceFromCentreOfScreen = length(TexCoords - 0.5);

            	vec4 color;

            	if (useChromaticAberration == 1)
            	{
            		vec2 rUV = TexCoords + chromAberIntensities.x * (TexCoords - 0.5);
            		vec2 gUV = TexCoords + chromAberIntensities.y * (TexCoords - 0.5);
            		vec2 bUV = TexCoords + chromAberIntensities.z * (TexCoords - 0.5);

                    color.r = texture(screenTexture, rUV).r;
            		color.g = texture(screenTexture, gUV).g;
            		color.b = texture(screenTexture, bUV).b;
            	}
            	else
            		color = texture(screenTexture, TexCoords);

            	/*float vignetteColor = 0;
            	if (useVignette == 1)
            		vignetteColor = (distanceFromCentreOfScreen) * vignetteStrength;

            	color -= vignetteColor;
            	color.a = 1;*/
                if (useDOF == 1)
                {
                    vec4 depth = texture(depthTexture, TexCoords);
                    float linearizedDepth = nearPlane * farPlane / (farPlane + texture(depthTexture, TexCoords).r * (nearPlane - farPlane));
                    float distanceCameraToObject = linearizedDepth;
                    float amountOfDistance = clamp(abs(DOFFocusPoint - distanceCameraToObject), 0, 1);
                    if (abs(DOFFocusPoint - distanceCameraToObject) < 3)
                    amountOfDistance = 0;

                    vec3 bestFocus = texture(screenTexture, TexCoords).rgb;
                    vec3 worstFocus = (textureLod(screenTexture, TexCoords, 2 * amountOfDistance * DOFStrength).rgb+ bestFocus) / 2.1;

                    color = vec4(worstFocus, 1);
                }
                color.a = 1;
                
            	FragColor = color;
            }
            """;

        private static string pickVertexShader =
            """
            #version 430 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTexCoords;
            layout (location = 2) in vec3 aNormal;

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            uniform mat4 model;

            void main()
            {
                gl_Position = vec4(aPos, 1) * model * view * projection;
            }
            """;

        private static string defaultVertexShaderText =
            """
            #version 430 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTexCoords;
            layout (location = 2) in vec3 aNormal;
            layout (location = 3) in ivec4 bonesID1;
            layout (location = 4) in ivec4 bonesID2;
            layout (location = 5) in vec4 weights1;
            layout (location = 6) in vec4 weights2;

            #define MAX_BONES 8
            #define MAX_TOTAL_BONES 128

            layout (std140, binding = 0) uniform Matrices
            {
            	mat4 projection;
            	mat4 view;
            };

            out mat4 Model;

            uniform mat4 model;
            uniform mat4 boneMatrices[MAX_TOTAL_BONES];
            out vec3 Normal;
            out vec2 TexCoords;
            out vec3 FragPos;

            void main() 
            {
                int bonesID[8] = { bonesID1[0], bonesID1[1], bonesID1[2], bonesID1[3], bonesID2[0], bonesID2[1], bonesID2[2], bonesID2[3] };
                float weights[8] = { weights1[0], weights1[1], weights1[2], weights1[3], weights2[0], weights2[1], weights2[2], weights2[3] };

                vec4 pos = (vec4(aPos, 1) * model);

                vec4 finalPos = vec4(0);
                for (int i = 0; i < MAX_BONES; i++)
                {
                    if (bonesID[i] == -1)
                        continue;
                    if (bonesID[i] >= MAX_TOTAL_BONES)
                    {
                        finalPos = vec4(aPos, 1);
                        break;
                    }
                    vec4 localPos = boneMatrices[bonesID[i]] * pos;
                    finalPos += localPos * weights[i];
                    vec3 localNorm  = mat3(boneMatrices[bonesID[i]]) * aNormal;
                }
                if (finalPos == vec4(0))
                    finalPos = pos;

            	FragPos = finalPos.xyz;//(finalPos * model).xyz;
            	Normal = aNormal * mat3(transpose(inverse(model))); //way more efficient if calculated on CPU
            	Model = model;
                TexCoords = aTexCoords;

            	gl_Position = finalPos * view * projection;
                //if (bonesID[0] == 0)
                //gl_Position = vec4(aPos, 1) * boneMatrices[0] * view * projection;
            }
            """;

        private static string defaultLightingShaderText =
            """
            #version 430 core
            out vec4 FragColor;

            #define PI 3.14159265359

            uniform vec3 viewPos;
            uniform vec3 lightPos;
            uniform float transparency;
            uniform int allowAlpha;
            uniform vec3 overrideColor;
            uniform int hasNormalMap;

            in vec2 TexCoords;

            in vec3 Normal;
            in vec3 FragPos;
            in mat4 Model;

            uniform sampler2D diffuseMap;
            uniform sampler2D specularMap;
            uniform sampler2D normalMap;
            uniform sampler2D metalMap;
            uniform sampler2D aoMap;
            uniform sampler2D displacementMap;

            uniform samplerCube skybox;

            #define heightScale 0.1

            vec3 getNormalFromMap()
            {
                vec3 normalMapColor = texture(normalMap, TexCoords).rgb;
                //normalMapColor.y = -normalMapColor.y;
                vec3 tangentNormal = normalMapColor * 2.0 - 1.0;

                vec3 Q1  = dFdx(FragPos);
                vec3 Q2  = dFdy(FragPos);
                vec2 st1 = dFdx(TexCoords);
                vec2 st2 = dFdy(TexCoords);

                vec3 N   = normalize(Normal);
                vec3 T  = normalize(Q1*st2.t + Q2*st1.t);
                vec3 B  = -normalize(cross(N, T));
                mat3 TBN = mat3(T, B, N);

                return normalize(TBN * tangentNormal);
            }
            vec2 ParallaxMapping(vec2 texCoords, vec3 viewDir)
            { 
                    float height =  texture(displacementMap, texCoords).r;     
                    return texCoords - viewDir.xy * (height * heightScale); 
            }
            void main()
            {
                vec3 viewDir = normalize((viewPos - FragPos));
                vec2 texCoords = TexCoords;//ParallaxMapping(TexCoords, viewDir); 

                if(texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0)
                    discard;

            	vec4 fullColor = texture(diffuseMap, texCoords);
            	if (fullColor.a < 0.1)
            		discard;
                    if (overrideColor != vec3(0))
                    {
                        vec3 lightDir = normalize((lightPos - FragPos));
                        vec3 normal = getNormalFromMap();
                        //vec3 viewDir = normalize((viewPos - FragPos));
                	    //vec3 reflectDir = reflect(-lightDir, normal);
                	    vec3 halfwayDir = normalize((lightDir + viewDir));
            	        float spec = pow(max(dot(normal, halfwayDir), 0), 225);//float spec = pow(max(dot(normal, halfwayDir), 0), 1);
                	    vec3 specular = vec3(.3) * spec * texture(specularMap, texCoords).rgb; // assuming bright white light color

                        FragColor = vec4(specular, transparency);
                        return;
                    }

            	vec3 color = pow(texture(diffuseMap, texCoords).rgb, vec3(2.2));
                float metallic = 1 - texture(metalMap, texCoords).r;

                vec3 ViewPos = (vec4(viewPos, 1)).xyz;
                vec3 normal = getNormalFromMap();

                //metalness (test)
                vec3 I = normalize(FragPos - viewPos);
                vec3 R = reflect(I, normalize(normal));//

                vec3 reflection = texture(skybox, R).rgb * texture(aoMap, texCoords).r;
                float strength = normalize(length(reflection));//(reflection.r + reflection.g + reflection.b) / 3;
                vec3 reflectiveness = metallic * reflection * vec3(.3);

            	// ambient
                vec3 ambient = color * strength * texture(aoMap, texCoords).r;//(color * strength) * texture(aoMap, texCoords).r;

            	// diffuse
                float distance = distance(lightPos, FragPos);
                float attenuation = 1.0 / (distance);
                vec3 lightDir = normalize((lightPos - FragPos));

                float diff = max(dot(lightDir, normal), 0.0) * attenuation;
                vec3 diffuse = vec3(.7) * diff * pow(texture(diffuseMap, texCoords).rgb, vec3(2.2));

            	// specular
                vec3 halfwayDir = normalize((lightDir + viewDir));
            	float spec = pow(max(dot(normal, halfwayDir), 0), metallic * 255);//vec3 spec = FresnelSchlick(max(dot(normal, halfwayDir), 0), F0);//
                vec3 specular = vec3(1) * spec * texture(specularMap, texCoords).rgb * reflection;//texture(specularMap, texCoords).rgb * reflection; // assuming bright white light color

            	if (allowAlpha == 1 && transparency != 0)
            		FragColor = vec4(vec3(1), transparency);//
            	else
            		FragColor = vec4(/*ambient + diffuse + specular*/ specular + reflection, 1.0);//
            //vec3 I = normalize(FragPos - viewPos);
            //vec3 R = reflect(I, normalize(normal));
            //FragColor = vec4(texture(skybox, R).rgb, 1.0);
            }
            """;
    }
}