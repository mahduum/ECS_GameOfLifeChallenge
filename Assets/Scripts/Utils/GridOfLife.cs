using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.UIElements;

namespace Utils
{
    //grid must be multiple of 3
    //start with simple 1 level grid, each grid cell is 3 x 3 and has reference to adjoining grid cells 3x3
    //such that within one grid unit we can check for all 8 adjoining grid units for their extreme values
    //if we check neighbours for the entity in the corner
    
    //we can have each entity being grid of 5x5 but we only modify the internal 3x3 units, so within each entity we can process 9 cells
    //but we must be certain that when creating these entities we must ensure that cells 5x5 will overlap by 2 so we offset the center of each cell 5x5 by 3
    //the overlap is always 2, so the bigger the cell, the less repetition
    
    //create shared component for each grid segment, and then spawn entities per each of those shared components, but this way we need component lookup...
    
    //shared can be created in baker based on grid specs
    //filter entities by shader grid segment
    //avoid component lookup by using query within the settings query? I have entities and must build query for that group?
    
    //use entity query and filter that by shared filter, that way we can have component array or  ToComponentDataListAsync<T>(AllocatorManager.AllocatorHandle, out JobHandle)
    
    public struct GridCachedSection
    {
        public List<bool> CellStatusList;
        public int2 Offset;
        //convert centerIndex in list to local xy by stride and offset to get the position
    }
    
    public struct GridOfLife
    {
        // public enum CachedSectionSize//determines new state per shared component for
        // {
        //     _3x3,//single entity
        //     _5x5,//three entities
        //     _7x7,//five entities
        //     _8x8,//six
        //     _9x9,//seven entities
        //     _16x16,//fourteen
        // }

        public int Width { get; } 
        public int Height { get; }

        public int TotalCells => Width * Height; 
        public int SectionStride { get; }
        public int SectionStrideWithOverlap { get; }
        public int SectionsCount => (Width / SectionStride) * (Height / SectionStride);
        
        public int WidthSectionsCount => Width / SectionStride;//todo resolve if there is a rest, will have not fully filled border section, that will have to stitch itself, we must reference the data from the section on the opposite side

        public int HeightSectionCount => Height / SectionStride;

        public int SectionCellCountWithOverlap => SectionStrideWithOverlap * SectionStrideWithOverlap;
        
        //public CachedSectionSize SectionSizeType { get; }

        //private readonly bool[] _neighbourCellStatusArray;

        public const int NeighboursCount = 8;
        
        private static readonly int2[] Offsets =
        {
            new (-1, 1), new (0, 1), new (1, 1),
            new (-1, 0), new (1, 0),
            new (-1, -1), new (0, -1), new (1, -1)
        };

        public static int WrapModulo(int dividend, int modulus)
        {
            return ((dividend % modulus) + modulus) % modulus;
        }

        public GridOfLife(int width, int height)
        {
            Width = width;
            Height = height;
            //_neighbourCellStatusArray = new bool[width * height];
            //SectionSizeType = sectionSize;
            SectionStride = 6;
            SectionStrideWithOverlap = 8;
        }

        public void FillGrid()
        {
            
        }

        public bool[] GetNeighbours(int index)
        {
            var surroundedCoords = new int2(index % Width, index / Width);

            bool[] neighbours = new bool[8];
            int2 wrap = new int2(Width, Height);
            
            for (int i = 0; i < Offsets.Length; i++)
            {
                var neighbourCoords = surroundedCoords + Offsets[i];

                neighbourCoords.x = WrapModulo(neighbourCoords.x, wrap.x);
                neighbourCoords.y = WrapModulo(neighbourCoords.y, wrap.y);
                var neighbourIndex = neighbourCoords.y * Width + neighbourCoords.x;
                //neighbours[i] = _neighbourCellStatusArray[neighbourIndex];
            }

            return neighbours;
        }
        
        public int GetNeighbourIndex(int centerIndex, int neighbourNumber)
        {
            var surroundedCoords = new int2(centerIndex % Width, centerIndex / Width);

            int2 wrap = new int2(Width, Height);
            
            var neighbourCoords = surroundedCoords + Offsets[neighbourNumber];
            //wrap out of bounds
            neighbourCoords %= wrap;
            return neighbourCoords.y * Width + neighbourCoords.x;
        }
        
        public static int GetNeighbourIndex(int centerIndex, int neighbourNumber, int width, int height)
        {
            var centerCoords = new int2(centerIndex % width, centerIndex / width);

            int2 wrap = new int2(width, height);
            
            var neighbourCoords = centerCoords + Offsets[neighbourNumber];//todo wrong wrap, index -1
            //wrap out of bounds
            
            //wrap out of bounds
            if (neighbourCoords.x < 0 || neighbourCoords.x >= wrap.x)
            {
                neighbourCoords.x = WrapModulo(neighbourCoords.x, wrap.x);
            }
            
            if (neighbourCoords.y < 0 || neighbourCoords.y >= wrap.y)
            {
                neighbourCoords.y = WrapModulo(neighbourCoords.y, wrap.y);
            }
            
            return neighbourCoords.y * width + neighbourCoords.x;
        }

        public static int2 GetCoordsFromIndex(int index, int gridWidth, int gridHeight)
        {
            if (index >= gridHeight * gridWidth)
            {
                throw new IndexOutOfRangeException("Cannot convert centerIndex to coordinates!");
            }
            return new int2(index % gridHeight, index / gridWidth);
        }
        
        public int2 GetCoordsFromIndex(int index)
        {
            return GetCoordsFromIndex(index, Width, Height);
        }
        
        public static bool[] GetNeighboursNoBoundsCheck(int index, int width, int height, bool[] isAliveStatusArray)
        {
            var centerCoords = new int2(index % width, index / width);

            bool[] neighbours = new bool[8];
            
            for (int i = 0; i < Offsets.Length; i++)
            {
                var neighbourCoords = centerCoords + Offsets[i];
 
                var neighbourIndex = neighbourCoords.y * width + neighbourCoords.x;
                neighbours[i] = isAliveStatusArray[neighbourIndex];
            }

            return neighbours;
        }

        public List<GridCachedSection> GetCacheGridSections()//todo make it specialized for the various sizes
        {
            //default for _8x8
            //dissect, establish how many
            List<GridCachedSection> sections = new List<GridCachedSection>();
            
            for (int i = 0; i < SectionsCount; i++)//iterate on equally sized but then includemargin
            {
                //use to offset the values, change to xy
                var sectionOffset = new int2(i % Width, i / Width);
                var gridSize = new int2(Width, Height);
                //create section
                var gridSection = new GridCachedSection()
                {
                    CellStatusList = new List<bool>(),
                    Offset = sectionOffset
                };

                //iterate by number of elements in a section
                //offset
                for (int j = 0; j < SectionCellCountWithOverlap; j++)//todo section has more than needed to instantiate
                {
                    var localCoords = new int2(j % SectionStrideWithOverlap, j / SectionStrideWithOverlap);
                    var globalCoords = localCoords + sectionOffset;
                    globalCoords %= gridSize;
                    var globalIndex = globalCoords.y * Width + globalCoords.x;
                    
                    //update gridSection, grid can keep offset
                    bool cellStatus = true;//_neighbourCellStatusArray[globalIndex];
                    //we know what is the global centerIndex in the big array, but we need to remap indices
                    //we are filling locally left to right, to allow remapping back to original grid we must store offset int2 and conversion info about stride
                    gridSection.CellStatusList.Add(cellStatus);
                }
                
                //we have sections for 
                sections.Add(gridSection);
            }

            return sections;
        }
        
        //todo make it return level cells based on the settings, it knows how many group segments (assigned to shared) it has to return
        //and we can iterate over them and make shared components from them, each group will have coordinates of the cells also to accomodate for the placement of the 
        //entities, shared component will have : buffer of booleans, it will assign indices to entities components, so each entity knows which other entities to check
        //optionally we can process separately live and dead entities but it may not have much sense
        
        //since we are iterating by entities, each entity may have component with centerIndex to itself in the grid section, or it may have indices for all the neighbours
        //entity will have its own centerIndex and then we use offsets to get neighbours from the adjoining cells
        //or we can sort buffer based on entities indices
        //we only ever count living entities, so we may have a query inside a query, for living neighbours?
        
        /*
         * 1. Create grid sections, each section is a different shared component, each shared component has this sections general coordinates (for update)
         * 2. Each shared gets a slice of buffer with booleans which on init are filled with random values (grid can do this)
         * 3. Spawning system for each cell with write access (border cells are read only) creates an entity, and assigns to this entity the centerIndex it will have on the grid.
         * 4. On life update system each shared component will iterate over its entities component enableable IsAlive, each entity will count its living neighbour
         *      by reading from the shared buffer of grid cells booleans and will set its enabledREFRW accordingly if state has changed. That way buffers can be small (check sizes of small buffers). TODO: can small buffer fit on component?
         * 5. Must iterate mutably only on inner entities, these entities must be filtered.
         */
        
        /*
         * We create each grid section with a given centerIndex, when we create entities we must assign them a position
         * We can have one big shared component with grid global data: stride, width, height etc.
         *
         * TODO: Procedurally in a system create entities based on grid data, create a helper grid entity that will have a config data
         * and after creating shared components for grid segments may destroy itself, helper entity may be a system itself todo: check how config runs in firefighters
         *
         * Need object entity grid-config in scene, that will have references to prefabs and will be used as argument in system that spawns shared components
         */
    }
}